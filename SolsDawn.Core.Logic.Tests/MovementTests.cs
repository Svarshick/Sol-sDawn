using System.Runtime.CompilerServices;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Entities;
using Xunit;

namespace SolsDawn.Core.Logic.Tests;

internal static class GameplayAssemblyLoader
{
    [ModuleInitializer]
    internal static void Load() => _ = typeof(Adsr).Assembly;
}

[MotionModel(AllowedPhase.All)]
public class MarkerMotion : IMotionModel
{
    public float Value;

    public float Calculate(float velocity, Sign inDir, float dt, float controlledSpeed) => Value;
}

[AdsrModel(AllowedAdsrState.Attack | AllowedAdsrState.Decay | AllowedAdsrState.Sustain | AllowedAdsrState.Release)]
public class TargetEcho : IAdsrModel
{
    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed) => targetSpeed;
}

[AdsrModel(AllowedAdsrState.Attack | AllowedAdsrState.Decay | AllowedAdsrState.Sustain | AllowedAdsrState.Release)]
public class PrevInDirEcho : IAdsrModel
{
    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed) => prevInDir;
}

[Collection("Library")]
public sealed class MovementTests : IDisposable
{
    private const float Dt = 0.1f;
    private const float C = 10f;

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

    private static Adsr NewAdsr(float acceleration = 100f, float peak = 15f, float deceleration = 100f)
    {
        var adsr = new Adsr { ControlledSpeed = C, PeakSpeed = peak };
        CategorySystem.Bind(adsr);
        Put<SimpleAttack, IAdsrModel>(adsr.Attack, m => m.Acceleration = acceleration);
        Put<LinearSlowdown, IAdsrModel>(adsr.Decay, m => m.Deceleration = deceleration);
        Put<LinearSlowdown, IAdsrModel>(adsr.Release, m => m.Deceleration = deceleration);
        return adsr;
    }

    private static AR NewAr(float peak = 15f, float acceleration = 30f, float deceleration = 30f)
    {
        var ar = new AR { PeakSpeed = peak };
        CategorySystem.Bind(ar);
        Put<SimpleAttack, IAdsrModel>(ar.Attack, m => m.Acceleration = acceleration);
        Put<LinearSlowdown, IAdsrModel>(ar.Release, m => m.Deceleration = deceleration);
        return ar;
    }

    private static AS NewAs(float peak = 15f, float acceleration = 30f)
    {
        var envelope = new AS { PeakSpeed = peak };
        CategorySystem.Bind(envelope);
        Put<SimpleAttack, IAdsrModel>(envelope.Attack, m => m.Acceleration = acceleration);
        return envelope;
    }

    private static MotionSolver NewSolver()
    {
        var solver = new MotionSolver { ControlledSpeed = C };
        CategorySystem.Bind(solver);
        Put<MarkerMotion, IMotionModel>(solver.GreatSpeedDrive, m => m.Value = 1);
        Put<MarkerMotion, IMotionModel>(solver.GreatSpeedBrake, m => m.Value = 2);
        Put<MarkerMotion, IMotionModel>(solver.GreatSpeedTurn, m => m.Value = 3);
        Put<MarkerMotion, IMotionModel>(solver.NormalDrive, m => m.Value = 4);
        Put<MarkerMotion, IMotionModel>(solver.NormalBrake, m => m.Value = 5);
        Put<MarkerMotion, IMotionModel>(solver.NormalTurn, m => m.Value = 6);
        return solver;
    }

    private static MovementMemory Memory(AdsrState state) => new() { Phase = (int)state };

    private static AdsrState State(MovementMemory memory) => (AdsrState)memory.Phase;

    private static List<AdsrState> Run(Adsr adsr, ref float velocity, ref MovementMemory memory, int inDir, int frames)
    {
        var states = new List<AdsrState>();
        for (var i = 0; i < frames; i++)
        {
            velocity = adsr.Calculate(velocity, inDir, Dt, ref memory);
            if (states.Count == 0 || states[^1] != State(memory))
                states.Add(State(memory));
        }

        return states;
    }

    [Fact]
    public void MOV05_MemoryIsIndependentForTheSameModel()
    {
        var adsr = NewAdsr(peak: 100f);
        var attacking = Memory(AdsrState.Attack);
        var other = Memory(AdsrState.Idle);

        adsr.Calculate(12f, 1, Dt, ref attacking);
        adsr.Calculate(12f, 1, Dt, ref other);

        Assert.Equal(AdsrState.Attack, State(attacking));
        Assert.Equal(AdsrState.Decay, State(other));
    }

    [Theory]
    [InlineData(typeof(Adsr))]
    [InlineData(typeof(MotionSolver))]
    [InlineData(typeof(AR))]
    [InlineData(typeof(AS))]
    public void MOV05_MovementModelsHaveNoRuntimeMembers(Type modelType)
    {
        var entry = Library.CreateCopyFromDefault(modelType);
        Library.SaveModel(entry.Guid);

        Assert.DoesNotContain("Phase", File.ReadAllText(Path.Combine(_directory, entry.Guid + ".json")));
        Assert.Null(modelType.GetProperty("Phase"));
        Assert.Null(modelType.GetProperty("State"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    public void MOV06_AnyMemoryValueIsAccepted(int phase)
    {
        foreach (IMovementModel model in new IMovementModel[] { NewAdsr(), NewSolver(), NewAr(), NewAs() })
        {
            foreach (var inDir in new[] { -1, 0, 1 })
            {
                foreach (var velocity in new[] { -20f, 0f, 5f, C })
                {
                    var memory = new MovementMemory { Phase = phase };
                    model.Calculate(velocity, inDir, Dt, ref memory);
                }
            }
        }
    }

    [Fact]
    public void MOV10_ZeroMemoryMeansDrive()
    {
        var solver = NewSolver();
        var memory = new MovementMemory();

        Assert.Equal(4f, solver.Calculate(0f, 1, Dt, ref memory));
        Assert.Equal(MovePhase.Drive, (MovePhase)memory.Phase);
    }

    [Theory]
    [InlineData(5f, 0, MovePhase.Drive, 5f, MovePhase.Brake)]
    [InlineData(15f, 0, MovePhase.Turn, 2f, MovePhase.Brake)]
    [InlineData(10f, 1, MovePhase.Turn, 4f, MovePhase.Drive)]
    [InlineData(15f, 1, MovePhase.Drive, 1f, MovePhase.Drive)]
    [InlineData(5f, -1, MovePhase.Drive, 6f, MovePhase.Turn)]
    [InlineData(15f, -1, MovePhase.Drive, 3f, MovePhase.Turn)]
    [InlineData(5f, 1, MovePhase.Turn, 6f, MovePhase.Turn)]
    [InlineData(5f, 1, MovePhase.Drive, 4f, MovePhase.Drive)]
    public void MOV11_MOV12_MOV13_MotionSolverPicksSlot(float velocity, int inDir, MovePhase start, float expected, MovePhase expectedPhase)
    {
        var solver = NewSolver();
        var memory = new MovementMemory { Phase = (int)start };

        Assert.Equal(expected, solver.Calculate(velocity, inDir, Dt, ref memory));
        Assert.Equal(expectedPhase, (MovePhase)memory.Phase);
    }

    [Fact]
    public void MOV15_MotionSolverPhaseModels()
    {
        Assert.Equal(-14f, new FrictionGreatSpeedDrive { Friction = 10f }.Calculate(15f, -1, Dt, C), 3);
        Assert.Equal(15f, new KeepGreatSpeedDrive().Calculate(15f, -1, Dt, C), 3);
        Assert.Equal(14f, new FrictionGreatSpeedDecay { Deceleration = 10f }.Calculate(15f, -1, Dt, C), 3);
        Assert.Equal(-6f, new LinearDriveModel { Acceleration = 10f }.Calculate(5f, -1, Dt, C), 3);
        Assert.Equal(0f, new LinearBrakeModel { Friction = 100f }.Calculate(5f, 0, Dt, C), 3);
        Assert.Equal(2f, new LinearTurnModel { TurnAcceleration = 30f }.Calculate(5f, -1, Dt, C), 3);
        Assert.Equal(-C, new LinearTurnModel { TurnAcceleration = 30f }.Calculate(-9f, -1, Dt, C), 3);
    }

    [Theory]
    [InlineData(AdsrState.Attack, 12f, 1, AdsrState.Attack)]
    [InlineData(AdsrState.Attack, 10f, 1, AdsrState.Attack)]
    [InlineData(AdsrState.Decay, 12f, 1, AdsrState.Decay)]
    [InlineData(AdsrState.Idle, 12f, 1, AdsrState.Decay)]
    [InlineData(AdsrState.Attack, 12f, 0, AdsrState.Decay)]
    [InlineData(AdsrState.Release, 12f, 0, AdsrState.Decay)]
    [InlineData(AdsrState.Decay, 10f, 1, AdsrState.Sustain)]
    [InlineData(AdsrState.Release, 10f, 1, AdsrState.Sustain)]
    [InlineData(AdsrState.Release, 0f, 0, AdsrState.Idle)]
    [InlineData(AdsrState.Idle, 0f, 0, AdsrState.Idle)]
    [InlineData(AdsrState.Idle, 0f, 1, AdsrState.Attack)]
    [InlineData(AdsrState.Release, 5f, 1, AdsrState.Attack)]
    [InlineData(AdsrState.Sustain, 5f, 1, AdsrState.Attack)]
    [InlineData(AdsrState.Attack, 5f, 0, AdsrState.Release)]
    [InlineData(AdsrState.Sustain, 10f, 0, AdsrState.Release)]
    [InlineData(AdsrState.Decay, 10f, 0, AdsrState.Release)]
    public void MOV34_StateSelection(AdsrState start, float speed, int inDir, AdsrState expected)
    {
        var adsr = NewAdsr(acceleration: 1f, peak: 100f, deceleration: 1f);
        var right = Memory(start);
        var left = Memory(start);

        adsr.Calculate(speed, inDir, Dt, ref right);
        adsr.Calculate(-speed, -inDir, Dt, ref left);

        Assert.Equal(expected, State(right));
        Assert.Equal(expected, State(left));
    }

    [Theory]
    [InlineData(9.9995f, 1, 10f, AdsrState.Sustain)]
    [InlineData(10.0005f, 1, 10f, AdsrState.Sustain)]
    [InlineData(-9.9995f, -1, -10f, AdsrState.Sustain)]
    [InlineData(0.0005f, 0, 0f, AdsrState.Idle)]
    public void MOV33_SpeedWithinToleranceIsSnapped(float velocity, int inDir, float expected, AdsrState expectedState)
    {
        var adsr = NewAdsr();
        var memory = new MovementMemory();

        Assert.Equal(expected, adsr.Calculate(velocity, inDir, Dt, ref memory));
        Assert.Equal(expectedState, State(memory));
    }

    [Fact]
    public void MOV33_SpeedOutsideToleranceIsNotSnapped()
    {
        var adsr = NewAdsr(acceleration: 1f);
        var memory = Memory(AdsrState.Sustain);

        adsr.Calculate(9.99f, 1, Dt, ref memory);

        Assert.Equal(AdsrState.Attack, State(memory));
    }

    [Fact]
    public void MOV31_MOV35_ZeroMemoryIsIdleAndIdleStopsMovement()
    {
        var adsr = NewAdsr();
        var memory = new MovementMemory();

        Assert.Equal(AdsrState.Idle, State(memory));
        Assert.Equal(0f, adsr.Calculate(0f, 0, Dt, ref memory));
        Assert.Equal(AdsrState.Idle, State(memory));
    }

    [Theory]
    [InlineData(15f, AdsrState.Idle, 5f, 1, 15f)]
    [InlineData(5f, AdsrState.Idle, 5f, 1, C)]
    [InlineData(15f, AdsrState.Idle, 12f, 1, C)]
    [InlineData(15f, AdsrState.Idle, 10f, 1, C)]
    [InlineData(15f, AdsrState.Idle, 5f, 0, 0f)]
    public void MOV35_SlotsReceiveTargetSpeed(float peak, AdsrState start, float velocity, int inDir, float expected)
    {
        var adsr = new Adsr { ControlledSpeed = C, PeakSpeed = peak };
        CategorySystem.Bind(adsr);
        Put<TargetEcho, IAdsrModel>(adsr.Attack);
        Put<TargetEcho, IAdsrModel>(adsr.Decay);
        Put<TargetEcho, IAdsrModel>(adsr.Sustain);
        Put<TargetEcho, IAdsrModel>(adsr.Release);
        var memory = Memory(start);

        Assert.Equal(expected, adsr.Calculate(velocity, inDir, Dt, ref memory));
    }

    [Fact]
    public void MOV34_AttackRunsUntilLimitThenDecays()
    {
        var adsr = NewAdsr(acceleration: 100f, peak: 15f);
        var memory = Memory(AdsrState.Attack);

        var velocity = adsr.Calculate(10f, 1, Dt, ref memory);
        Assert.Equal(15f, velocity);
        Assert.Equal(AdsrState.Attack, State(memory));

        adsr.Calculate(velocity, 1, Dt, ref memory);
        Assert.Equal(AdsrState.Decay, State(memory));
    }

    [Fact]
    public void MOV34_PeakBelowControlledSpeedStillReachesControlledSpeed()
    {
        var adsr = NewAdsr(acceleration: 100f, peak: 5f);
        var memory = new MovementMemory();

        var velocity = adsr.Calculate(4f, 1, Dt, ref memory);
        Assert.Equal(C, velocity);

        adsr.Calculate(velocity, 1, Dt, ref memory);
        Assert.Equal(AdsrState.Sustain, State(memory));
    }

    [Theory]
    [InlineData(AdsrState.Sustain, -1, C, 1, -1)]
    [InlineData(AdsrState.Release, -1, C, 1, 1)]
    [InlineData(AdsrState.Idle, 0, 5f, -1, -1)]
    public void MOV38_PrevInDirIsInDirOnTheFirstFrameOfAPhase(AdsrState start, int storedPrev, float velocity, int inDir, int expected)
    {
        var adsr = new Adsr { ControlledSpeed = C };
        CategorySystem.Bind(adsr);
        Put<PrevInDirEcho, IAdsrModel>(adsr.Attack);
        Put<PrevInDirEcho, IAdsrModel>(adsr.Decay);
        Put<PrevInDirEcho, IAdsrModel>(adsr.Sustain);
        Put<PrevInDirEcho, IAdsrModel>(adsr.Release);
        var memory = new MovementMemory { Phase = (int)start, PrevInDir = storedPrev };

        Assert.Equal(expected, adsr.Calculate(velocity, inDir, Dt, ref memory));
        Assert.Equal(inDir, (int)memory.PrevInDir);
    }

    [Theory]
    [InlineData(AdsrState.Attack, 5f)]
    [InlineData(AdsrState.Decay, 12f)]
    [InlineData(AdsrState.Sustain, 10f)]
    public void MOV36_MOV41_TurningKeepsStateAndSpeed(AdsrState state, float speed)
    {
        var adsr = NewAdsr(acceleration: 0f, peak: 100f, deceleration: 0f);
        var memory = Memory(state);

        var velocity = adsr.Calculate(speed, -1, Dt, ref memory);

        Assert.Equal(-speed, velocity);
        Assert.Equal(state, State(memory));
    }

    [Fact]
    public void MOV39_MOV41_LinearSlowdownStopsExactlyAtTarget()
    {
        var model = new LinearSlowdown { Deceleration = 100f };

        Assert.Equal(C, model.Calculate(12f, 1, 1, Dt, C));
        Assert.Equal(0f, model.Calculate(5f, 0, 0, Dt, 0f));
        Assert.Equal(-9f, new LinearSlowdown { Deceleration = 10f }.Calculate(-10f, 0, 0, Dt, 0f), 3);
    }

    [Fact]
    public void MOV39_MOV41_SimpleAttackStopsExactlyAtTarget()
    {
        var model = new SimpleAttack { Acceleration = 100f };

        Assert.Equal(C, model.Calculate(9f, 1, 1, Dt, C));
        Assert.Equal(-1f, new SimpleAttack { Acceleration = 10f }.Calculate(0f, -1, -1, Dt, C), 3);
    }

    [Fact]
    public void MOV41_KeepSpeedPreservesSpeedAndFollowsInput()
    {
        var model = new KeepSpeed();

        Assert.Equal(-C, model.Calculate(C, -1, -1, Dt, C));
        Assert.Equal(C, model.Calculate(C, 0, 0, Dt, C));
    }

    [Fact]
    public void MOV52_MOV53_ArRisesToPeakThenReleasesToDone()
    {
        var ar = NewAr(peak: 15f, acceleration: 30f, deceleration: 30f);
        var memory = new MovementMemory();
        var speed = 0f;
        var frames = new List<(float Speed, ArPhase Phase)>();

        for (var i = 0; i < 20 && !AR.IsDone(memory); i++)
        {
            speed = ar.Calculate(speed, 1, Dt, ref memory);
            frames.Add((speed, (ArPhase)memory.Phase));
        }

        Assert.Equal(10, frames.Count);
        Assert.Equal((15f, ArPhase.Attack), (MathF.Round(frames[4].Speed, 3), frames[4].Phase));
        Assert.Equal(ArPhase.Release, frames[5].Phase);
        Assert.Equal((0f, ArPhase.Done), frames[^1]);
    }

    [Fact]
    public void MOV52_ArDoneStaysDoneAtZero()
    {
        var ar = NewAr();
        var memory = new MovementMemory { Phase = (int)ArPhase.Done };

        Assert.Equal(0f, ar.Calculate(7f, 1, Dt, ref memory));
        Assert.True(AR.IsDone(memory));
    }

    [Fact]
    public void MOV57_ArEnteredAtPeakSkipsAttack()
    {
        var ar = NewAr(peak: 15f, deceleration: 10f);
        var memory = new MovementMemory();

        Assert.Equal(19f, ar.Calculate(20f, 1, Dt, ref memory), 3);
        Assert.Equal(ArPhase.Release, (ArPhase)memory.Phase);
    }

    [Fact]
    public void MOV55_AsRisesToPeakThenSustainsForever()
    {
        var envelope = NewAs(peak: 15f, acceleration: 30f);
        var memory = new MovementMemory();
        var speed = 0f;

        for (var i = 0; i < 50; i++)
            speed = envelope.Calculate(speed, 1, Dt, ref memory);

        Assert.Equal(15f, speed, 3);
        Assert.Equal(AsPhase.Sustain, (AsPhase)memory.Phase);
    }

    [Fact]
    public void MOV57_AsEnteredAbovePeakKeepsSpeed()
    {
        var envelope = NewAs(peak: 15f);
        var memory = new MovementMemory();

        Assert.Equal(20f, envelope.Calculate(20f, 1, Dt, ref memory));
        Assert.Equal(AsPhase.Sustain, (AsPhase)memory.Phase);
    }

    [Fact]
    public void MOV56_EnvelopeFirstFrameGetsInDirAsPrevInDir()
    {
        var ar = new AR { PeakSpeed = 15f };
        CategorySystem.Bind(ar);
        Put<PrevInDirEcho, IAdsrModel>(ar.Attack);
        var memory = new MovementMemory();

        Assert.Equal(-1f, ar.Calculate(0f, -1, Dt, ref memory));
        Assert.Equal(ArPhase.Attack, (ArPhase)memory.Phase);
    }

    [Fact]
    public void Example_HoldFromRestThenReleaseFromSustain()
    {
        var adsr = NewAdsr(acceleration: 30f, peak: 15f, deceleration: 30f);
        var memory = new MovementMemory();
        var velocity = 0f;

        Assert.Equal(
            [AdsrState.Attack, AdsrState.Decay, AdsrState.Sustain],
            Run(adsr, ref velocity, ref memory, 1, 20));
        Assert.Equal(C, velocity);

        Assert.Equal(
            [AdsrState.Release, AdsrState.Idle],
            Run(adsr, ref velocity, ref memory, 0, 20));
        Assert.Equal(0f, velocity);
    }

    [Fact]
    public void Example_ReleaseDuringAttackAboveControlledSpeedDecaysThenReleases()
    {
        var adsr = NewAdsr(acceleration: 30f, peak: 100f, deceleration: 30f);
        var memory = Memory(AdsrState.Attack);
        var velocity = 12f;

        Assert.Equal(
            [AdsrState.Decay, AdsrState.Release, AdsrState.Idle],
            Run(adsr, ref velocity, ref memory, 0, 20));
    }

    [Fact]
    public void Example_EnteredAboveControlledSpeedWhileHeld()
    {
        var adsr = NewAdsr(deceleration: 30f);
        var memory = new MovementMemory();
        var velocity = 20f;

        Assert.Equal(
            [AdsrState.Decay, AdsrState.Sustain],
            Run(adsr, ref velocity, ref memory, 1, 20));
        Assert.Equal(C, velocity);
    }
}
