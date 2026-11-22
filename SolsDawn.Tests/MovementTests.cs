using System.Runtime.CompilerServices;
using Xunit;

namespace SolsDawn.Tests;

internal static class GameplayAssemblyLoader
{
    [ModuleInitializer]
    internal static void Load() => _ = typeof(SolsDawn.Gameplay.GameConfig).Assembly;
}

[AdsrModel(AllowedAdsrState.Attack)]
public class TestAttack : IAdsrModel
{
    public float Acceleration = 10f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) =>
        Math.Min(toSpeed, speed + Acceleration * dt);
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class TestSlowdown : IAdsrModel
{
    public float Deceleration = 10f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) =>
        Math.Max(toSpeed, speed - Deceleration * dt);
}

[AdsrModel(AllowedAdsrState.Attack | AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class FromEcho : IAdsrModel
{
    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) => fromSpeed;
}

[AdsrModel(AllowedAdsrState.Attack | AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class ToEcho : IAdsrModel
{
    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) => toSpeed;
}

[Collection("Library")]
public sealed class MovementTests : IDisposable
{
    private const float Dt = 0.1f;
    private const float C = 10f;
    private const float P = 15f;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "solsdawn-" + Guid.NewGuid().ToString("N"));

    public MovementTests() => Library.Initialize(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static TModel Put<TModel, TSlot>(CategorySlot<TSlot> slot, Action<TModel>? setup = null)
        where TModel : class, TSlot
        where TSlot : class
    {
        var entry = Library.CreateCopyFromDefault(typeof(TModel));
        var model = (TModel)entry.Instance!;
        setup?.Invoke(model);
        slot.ReferenceModel(entry.Guid);
        return model;
    }

    private static void PutTurn<TAttack, TSlowdown>(CategorySlot<Dra> slot, float acceleration, float deceleration)
        where TAttack : class, IAdsrModel
        where TSlowdown : class, IAdsrModel
    {
        var dra = Put<Dra, Dra>(slot);
        Put<TSlowdown, IAdsrModel>(dra.Decay, m => SetRate(m, deceleration));
        Put<TSlowdown, IAdsrModel>(dra.Release, m => SetRate(m, deceleration));
        Put<TAttack, IAdsrModel>(dra.Attack, m => SetRate(m, acceleration));
    }

    private static void SetRate(IAdsrModel model, float rate)
    {
        if (model is TestAttack attack)
            attack.Acceleration = rate;
        if (model is TestSlowdown slowdown)
            slowdown.Deceleration = rate;
    }

    private static Adsr NewAdsr(float acceleration = 100f, float deceleration = 100f, float peak = P)
    {
        var adsr = new Adsr { ControlledSpeed = C, PeakSpeed = peak };
        CategorySystem.Bind(adsr);
        Put<TestAttack, IAdsrModel>(adsr.Attack, m => m.Acceleration = acceleration);
        Put<TestSlowdown, IAdsrModel>(adsr.Decay, m => m.Deceleration = deceleration);
        Put<TestSlowdown, IAdsrModel>(adsr.Release, m => m.Deceleration = deceleration);
        PutTurn<TestAttack, TestSlowdown>(adsr.Turn, acceleration, deceleration);
        return adsr;
    }

    private static Ads NewAds(float acceleration = 100f, float deceleration = 100f, float peak = P)
    {
        var ads = new Ads { PeakSpeed = peak };
        CategorySystem.Bind(ads);
        Put<TestAttack, IAdsrModel>(ads.Attack, m => m.Acceleration = acceleration);
        Put<TestSlowdown, IAdsrModel>(ads.Decay, m => m.Deceleration = deceleration);
        PutTurn<TestAttack, TestSlowdown>(ads.Turn, acceleration, deceleration);
        return ads;
    }

    private static Adsr EchoAdsr<TEcho>() where TEcho : class, IAdsrModel
    {
        var adsr = new Adsr { ControlledSpeed = C, PeakSpeed = P };
        CategorySystem.Bind(adsr);
        Put<TEcho, IAdsrModel>(adsr.Attack);
        Put<TEcho, IAdsrModel>(adsr.Decay);
        Put<TEcho, IAdsrModel>(adsr.Release);
        PutTurn<TEcho, TEcho>(adsr.Turn, 0f, 0f);
        return adsr;
    }

    private static MovementMemory Memory(AdsrPhase phase) => new() { Phase = (int)phase };

    private static AdsrPhase Phase(MovementMemory memory) => (AdsrPhase)memory.Phase;

    private static List<AdsrPhase> Run(IMovementModel model, ref float velocity, ref MovementMemory memory, int inputDir, int frames)
    {
        var phases = new List<AdsrPhase>();
        for (var i = 0; i < frames; i++)
        {
            velocity = model.Calculate(velocity, inputDir, Dt, ref memory);
            if (phases.Count == 0 || phases[^1] != Phase(memory))
                phases.Add(Phase(memory));
        }

        return phases;
    }

    [Fact]
    public void MOV05_MemoryIsIndependentForTheSameModel()
    {
        var adsr = NewAdsr();
        var attacking = Memory(AdsrPhase.Attack);
        var other = Memory(AdsrPhase.Idle);

        adsr.Calculate(12f, 1, Dt, ref attacking);
        adsr.Calculate(12f, 1, Dt, ref other);

        Assert.Equal(AdsrPhase.Attack, Phase(attacking));
        Assert.Equal(AdsrPhase.Decay, Phase(other));
    }

    [Theory]
    [InlineData(typeof(Adsr))]
    [InlineData(typeof(Ads))]
    [InlineData(typeof(Dra))]
    public void MOV05_ModelsHaveNoRuntimeMembers(Type modelType)
    {
        var entry = Library.CreateCopyFromDefault(modelType);
        Library.MarkDirty(entry.Guid);
        Library.Save();

        Assert.DoesNotContain("Phase", File.ReadAllText(Path.Combine(_directory, entry.Guid + ".json")));
        Assert.Null(modelType.GetProperty("Phase"));
        Assert.Null(modelType.GetProperty("State"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(99)]
    public void MOV06_AnyMemoryValueIsAccepted(int phase)
    {
        foreach (IMovementModel model in new IMovementModel[] { NewAdsr(), NewAds() })
        foreach (var inputDir in new[] { -1, 0, 1 })
        foreach (var velocity in new[] { -20f, 0f, 5f, C, P })
        {
            var memory = new MovementMemory { Phase = phase };
            model.Calculate(velocity, inputDir, Dt, ref memory);
        }
    }

    [Fact]
    public void MOV06_UnknownPhaseIsTreatedAsIdle()
    {
        var adsr = NewAdsr(acceleration: 1f);
        var unknown = new MovementMemory { Phase = 99 };
        var idle = Memory(AdsrPhase.Idle);

        Assert.Equal(adsr.Calculate(12f, 1, Dt, ref idle), adsr.Calculate(12f, 1, Dt, ref unknown));
        Assert.Equal(Phase(idle), Phase(unknown));
    }

    [Theory]
    [InlineData(AdsrPhase.TurnAttack, 5f, 1, AdsrPhase.TurnAttack)]
    [InlineData(AdsrPhase.TurnAttack, 12f, 1, AdsrPhase.TurnAttack)]
    [InlineData(AdsrPhase.Idle, 12f, -1, AdsrPhase.TurnDecay)]
    [InlineData(AdsrPhase.Attack, 12f, -1, AdsrPhase.TurnDecay)]
    [InlineData(AdsrPhase.TurnAttack, 12f, -1, AdsrPhase.TurnDecay)]
    [InlineData(AdsrPhase.Sustain, 10f, -1, AdsrPhase.TurnRelease)]
    [InlineData(AdsrPhase.Attack, 5f, -1, AdsrPhase.TurnRelease)]
    [InlineData(AdsrPhase.Release, 5f, -1, AdsrPhase.TurnRelease)]
    [InlineData(AdsrPhase.TurnDecay, 10f, -1, AdsrPhase.TurnRelease)]
    [InlineData(AdsrPhase.TurnRelease, 0f, 1, AdsrPhase.TurnAttack)]
    [InlineData(AdsrPhase.TurnRelease, 0f, -1, AdsrPhase.TurnAttack)]
    [InlineData(AdsrPhase.TurnDecay, 0f, 1, AdsrPhase.TurnAttack)]
    [InlineData(AdsrPhase.Attack, 12f, 1, AdsrPhase.Attack)]
    [InlineData(AdsrPhase.Attack, 15f, 1, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.TurnAttack, 15f, 1, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.Idle, 12f, 1, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.TurnDecay, 12f, 1, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.TurnDecay, 12f, 0, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.Attack, 12f, 0, AdsrPhase.Decay)]
    [InlineData(AdsrPhase.Decay, 10f, 1, AdsrPhase.Sustain)]
    [InlineData(AdsrPhase.TurnRelease, 10f, 1, AdsrPhase.Sustain)]
    [InlineData(AdsrPhase.Release, 0f, 0, AdsrPhase.Idle)]
    [InlineData(AdsrPhase.TurnRelease, 0f, 0, AdsrPhase.Idle)]
    [InlineData(AdsrPhase.Idle, 0f, 0, AdsrPhase.Idle)]
    [InlineData(AdsrPhase.Idle, 0f, 1, AdsrPhase.Attack)]
    [InlineData(AdsrPhase.Idle, 0f, -1, AdsrPhase.Attack)]
    [InlineData(AdsrPhase.Sustain, 5f, 1, AdsrPhase.Attack)]
    [InlineData(AdsrPhase.TurnRelease, 5f, 1, AdsrPhase.Attack)]
    [InlineData(AdsrPhase.Attack, 5f, 0, AdsrPhase.Release)]
    [InlineData(AdsrPhase.TurnAttack, 5f, 0, AdsrPhase.Release)]
    [InlineData(AdsrPhase.TurnRelease, 5f, 0, AdsrPhase.Release)]
    [InlineData(AdsrPhase.Sustain, 10f, 0, AdsrPhase.Release)]
    public void MOV35_MOV36_PhaseSelection(AdsrPhase start, float velocity, int inputDir, AdsrPhase expected)
    {
        var adsr = NewAdsr(acceleration: 1f, deceleration: 1f);
        var right = Memory(start);
        var left = Memory(start);

        adsr.Calculate(velocity, inputDir, Dt, ref right);
        adsr.Calculate(-velocity, -inputDir, Dt, ref left);

        Assert.Equal(expected, Phase(right));
        Assert.Equal(expected, Phase(left));
    }

    [Theory]
    [InlineData(AdsrPhase.Idle, 5f, 1, 0f, P)]
    [InlineData(AdsrPhase.Idle, 20f, 1, P, C)]
    [InlineData(AdsrPhase.Idle, 5f, 0, C, 0f)]
    [InlineData(AdsrPhase.Idle, 20f, -1, P, C)]
    [InlineData(AdsrPhase.Idle, 5f, -1, C, 0f)]
    [InlineData(AdsrPhase.TurnRelease, 0f, 1, 0f, P)]
    public void MOV37_SlotsReceiveEdges(AdsrPhase start, float velocity, int inputDir, float from, float to)
    {
        var fromMemory = Memory(start);
        var toMemory = Memory(start);

        Assert.Equal(from, Math.Abs(EchoAdsr<FromEcho>().Calculate(velocity, inputDir, Dt, ref fromMemory)));
        Assert.Equal(to, Math.Abs(EchoAdsr<ToEcho>().Calculate(velocity, inputDir, Dt, ref toMemory)));
    }

    [Theory]
    [InlineData(-10f, -1, -10f)]
    [InlineData(0f, 0, 0f)]
    public void MOV37_SustainKeepsSpeedAndIdleStops(float velocity, int inputDir, float expected)
    {
        var adsr = EchoAdsr<ToEcho>();
        var memory = new MovementMemory();

        Assert.Equal(expected, adsr.Calculate(velocity, inputDir, Dt, ref memory));
    }

    [Theory]
    [InlineData(20f, -1, C)]
    [InlineData(5f, -1, 0f)]
    [InlineData(-20f, 1, -C)]
    [InlineData(5f, 0, 0f)]
    public void MOV38_TurnBrakesInTheOldDirection(float velocity, int inputDir, float expected)
    {
        var adsr = EchoAdsr<ToEcho>();
        var memory = new MovementMemory();

        Assert.Equal(expected, adsr.Calculate(velocity, inputDir, Dt, ref memory));
    }

    [Theory]
    [InlineData(1, P)]
    [InlineData(-1, -P)]
    public void MOV38_AtRestTheInputSetsTheDirection(int inputDir, float expected)
    {
        var adsr = EchoAdsr<ToEcho>();
        var memory = Memory(AdsrPhase.TurnRelease);

        Assert.Equal(expected, adsr.Calculate(0f, inputDir, Dt, ref memory));
        Assert.Equal(AdsrPhase.TurnAttack, Phase(memory));
    }

    [Theory]
    [InlineData(9.9995f, 1, 10f, AdsrPhase.Sustain)]
    [InlineData(10.0005f, 1, 10f, AdsrPhase.Sustain)]
    [InlineData(-9.9995f, -1, -10f, AdsrPhase.Sustain)]
    [InlineData(0.0005f, 0, 0f, AdsrPhase.Idle)]
    public void MOV34_SpeedWithinToleranceIsSnapped(float velocity, int inputDir, float expected, AdsrPhase expectedPhase)
    {
        var adsr = NewAdsr();
        var memory = new MovementMemory();

        Assert.Equal(expected, adsr.Calculate(velocity, inputDir, Dt, ref memory));
        Assert.Equal(expectedPhase, Phase(memory));
    }

    [Fact]
    public void MOV34_SpeedOutsideToleranceIsNotSnapped()
    {
        var adsr = NewAdsr(acceleration: 1f);
        var memory = Memory(AdsrPhase.Sustain);

        adsr.Calculate(9.99f, 1, Dt, ref memory);

        Assert.Equal(AdsrPhase.Attack, Phase(memory));
    }

    [Theory]
    [InlineData(nameof(Adsr.ControlledSpeed), 20f, 20f, 20f)]
    [InlineData(nameof(Adsr.PeakSpeed), 5f, 5f, 5f)]
    [InlineData(nameof(Adsr.ControlledSpeed), 12f, 12f, P)]
    [InlineData(nameof(Adsr.PeakSpeed), 12f, C, 12f)]
    public void MOV31_CFG66_EditSlidesTheOtherSpeed(string field, float value, float expectedC, float expectedP)
    {
        var entry = Library.CreateCopyFromDefault(typeof(Adsr));
        var adsr = (Adsr)entry.Instance!;
        adsr.ControlledSpeed = C;
        adsr.PeakSpeed = P;

        ModelEdit.ForModel(entry).SetField(typeof(Adsr).GetField(field)!, value);

        Assert.Equal(expectedC, adsr.ControlledSpeed);
        Assert.Equal(expectedP, adsr.PeakSpeed);
    }

    [Fact]
    public void MOV31_CFG66_LoadRaisesPeakToControlledSpeed()
    {
        var entry = Library.CreateCopyFromDefault(typeof(Adsr));
        var adsr = (Adsr)entry.Instance!;
        adsr.ControlledSpeed = 20f;
        adsr.PeakSpeed = 5f;
        Library.MarkDirty(entry.Guid);
        Library.Save();

        Library.Initialize(_directory);
        var loaded = (Adsr)Library.GetModelEntry(entry.Guid)!.Instance!;

        Assert.Equal(20f, loaded.ControlledSpeed);
        Assert.Equal(20f, loaded.PeakSpeed);
    }

    [Fact]
    public void Example_HoldFromRestThenRelease()
    {
        var adsr = NewAdsr(acceleration: 30f, deceleration: 30f);
        var memory = new MovementMemory();
        var velocity = 0f;

        Assert.Equal([AdsrPhase.Attack, AdsrPhase.Decay, AdsrPhase.Sustain], Run(adsr, ref velocity, ref memory, 1, 20));
        Assert.Equal(C, velocity);

        Assert.Equal([AdsrPhase.Release, AdsrPhase.Idle], Run(adsr, ref velocity, ref memory, 0, 20));
        Assert.Equal(0f, velocity);
    }

    [Fact]
    public void Example_ReleaseDuringAttackAboveControlledSpeedDecaysThenReleases()
    {
        var adsr = NewAdsr(acceleration: 30f, deceleration: 30f);
        var memory = Memory(AdsrPhase.Attack);
        var velocity = 12f;

        Assert.Equal([AdsrPhase.Decay, AdsrPhase.Release, AdsrPhase.Idle], Run(adsr, ref velocity, ref memory, 0, 20));
    }

    [Fact]
    public void Example_ReversingDuringAttackAboveControlledSpeedRunsTheWholeTurn()
    {
        var adsr = NewAdsr(acceleration: 30f, deceleration: 30f);
        var memory = Memory(AdsrPhase.Attack);
        var velocity = 12f;

        Assert.Equal(
            [AdsrPhase.TurnDecay, AdsrPhase.TurnRelease, AdsrPhase.TurnAttack, AdsrPhase.Decay, AdsrPhase.Sustain],
            Run(adsr, ref velocity, ref memory, -1, 40));
        Assert.Equal(-C, velocity);
    }

    [Fact]
    public void Example_BackToTheOldDirectionDuringTurnReturnsToAdsr()
    {
        var adsr = NewAdsr(acceleration: 30f, deceleration: 30f);
        var memory = Memory(AdsrPhase.Sustain);
        var velocity = C;

        Run(adsr, ref velocity, ref memory, -1, 1);
        Assert.Equal(AdsrPhase.TurnRelease, Phase(memory));
        Assert.True(velocity > 0f);

        Assert.Equal([AdsrPhase.Attack, AdsrPhase.Decay, AdsrPhase.Sustain], Run(adsr, ref velocity, ref memory, 1, 20));
        Assert.Equal(C, velocity);
    }

    [Fact]
    public void MOV39_InstantTurnStopsForOneFrameAndPassesThroughPeak()
    {
        var adsr = NewAdsr(acceleration: 1000f, deceleration: 1000f);
        var memory = Memory(AdsrPhase.Sustain);
        var velocity = C;

        velocity = adsr.Calculate(velocity, -1, Dt, ref memory);
        Assert.Equal(0f, velocity);
        velocity = adsr.Calculate(velocity, -1, Dt, ref memory);
        Assert.Equal(-P, velocity);
        velocity = adsr.Calculate(velocity, -1, Dt, ref memory);
        Assert.Equal(-C, velocity);
    }

    [Fact]
    public void MOV53_AdsTurnsAnOpposingVelocityIntoTheDirection()
    {
        var ads = NewAds(acceleration: 30f, deceleration: 30f);
        var memory = new MovementMemory();
        var velocity = -20f;

        Assert.Equal(
            [AdsrPhase.TurnDecay, AdsrPhase.TurnRelease, AdsrPhase.TurnAttack, AdsrPhase.Sustain],
            Run(ads, ref velocity, ref memory, 1, 40));
        Assert.Equal(P, velocity);
    }

    [Fact]
    public void MOV51_AdsAbovePeakDecaysToPeak()
    {
        var ads = NewAds(deceleration: 30f);
        var memory = new MovementMemory();
        var velocity = 20f;

        Assert.Equal([AdsrPhase.Decay, AdsrPhase.Sustain], Run(ads, ref velocity, ref memory, 1, 20));
        Assert.Equal(P, velocity);
    }

    [Theory]
    [InlineData(5f, AdsrPhase.Attack)]
    [InlineData(-5f, AdsrPhase.Attack)]
    [InlineData(0f, AdsrPhase.Idle)]
    public void MOV52_AdsTreatsZeroInputAsTheMotionDirection(float velocity, AdsrPhase expected)
    {
        var ads = NewAds(acceleration: 1f);
        var memory = new MovementMemory();

        var result = ads.Calculate(velocity, 0, Dt, ref memory);

        Assert.Equal(expected, Phase(memory));
        Assert.Equal(Math.Sign(velocity), Math.Sign(result));
    }
}
