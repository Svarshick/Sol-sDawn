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

    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed) => Value;
}

[AdsrModel(AllowedAdsrState.Attack)]
public class TargetEchoAttack : IAdsrAttackModel
{
    public float Calculate(float velocity, int inDir, float dt, float targetSpeed) => targetSpeed;

    public bool IsFinished(float speed, float targetSpeed) => false;
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Sustain | AllowedAdsrState.Release)]
public class TargetEcho : IAdsrModel
{
    public float Calculate(float velocity, int inDir, float dt, float targetSpeed) => targetSpeed;
}

[AdsrModel(AllowedAdsrState.Attack)]
public class AlwaysFinishedAttack : IAdsrAttackModel
{
    public float Calculate(float velocity, int inDir, float dt, float targetSpeed) => velocity + inDir;

    public bool IsFinished(float speed, float targetSpeed) => true;
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
        var adsr = new Adsr { ControlledSpeed = C };
        CategorySystem.Bind(adsr);
        Put<SimpleAttack, IAdsrAttackModel>(adsr.Attack, m =>
        {
            m.Acceleration = acceleration;
            m.PeakSpeed = peak;
        });
        Put<LinearSlowdown, IAdsrModel>(adsr.Decay, m => m.Deceleration = deceleration);
        Put<LinearSlowdown, IAdsrModel>(adsr.Release, m => m.Deceleration = deceleration);
        return adsr;
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
        foreach (IMovementModel model in new IMovementModel[] { NewAdsr(), NewSolver() })
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
    [InlineData(AdsrState.Idle, 5f, 1, C)]
    [InlineData(AdsrState.Idle, 12f, 1, C)]
    [InlineData(AdsrState.Idle, 10f, 1, C)]
    [InlineData(AdsrState.Idle, 5f, 0, 0f)]
    public void MOV35_SlotsReceiveTargetSpeed(AdsrState start, float velocity, int inDir, float expected)
    {
        var adsr = new Adsr { ControlledSpeed = C };
        CategorySystem.Bind(adsr);
        Put<TargetEchoAttack, IAdsrAttackModel>(adsr.Attack);
        Put<TargetEcho, IAdsrModel>(adsr.Decay);
        Put<TargetEcho, IAdsrModel>(adsr.Sustain);
        Put<TargetEcho, IAdsrModel>(adsr.Release);
        var memory = Memory(start);

        Assert.Equal(expected, adsr.Calculate(velocity, inDir, Dt, ref memory));
    }

    [Fact]
    public void MOV36_AttackFinishesIntoDecayAtItsLimitWhileHeld()
    {
        var adsr = NewAdsr(acceleration: 100f, peak: 15f);
        var memory = Memory(AdsrState.Attack);

        var velocity = adsr.Calculate(10f, 1, Dt, ref memory);

        Assert.Equal(15f, velocity);
        Assert.Equal(AdsrState.Decay, State(memory));
    }

    [Fact]
    public void MOV36_AttackNeverEndsBelowControlledSpeed()
    {
        var adsr = new Adsr { ControlledSpeed = C };
        CategorySystem.Bind(adsr);
        Put<AlwaysFinishedAttack, IAdsrAttackModel>(adsr.Attack);
        var memory = new MovementMemory();

        adsr.Calculate(5f, 1, Dt, ref memory);

        Assert.Equal(AdsrState.Attack, State(memory));
    }

    [Theory]
    [InlineData(AdsrState.Attack, 5f)]
    [InlineData(AdsrState.Decay, 12f)]
    [InlineData(AdsrState.Sustain, 10f)]
    public void MOV37_MOV44_TurningKeepsStateAndSpeed(AdsrState state, float speed)
    {
        var adsr = NewAdsr(acceleration: 0f, peak: 100f, deceleration: 0f);
        var memory = Memory(state);

        var velocity = adsr.Calculate(speed, -1, Dt, ref memory);

        Assert.Equal(-speed, velocity);
        Assert.Equal(state, State(memory));
    }

    [Fact]
    public void MOV42_MOV44_LinearSlowdownStopsExactlyAtTarget()
    {
        var model = new LinearSlowdown { Deceleration = 100f };

        Assert.Equal(C, model.Calculate(12f, 1, Dt, C));
        Assert.Equal(0f, model.Calculate(5f, 0, Dt, 0f));
        Assert.Equal(-9f, new LinearSlowdown { Deceleration = 10f }.Calculate(-10f, 0, Dt, 0f), 3);
    }

    [Fact]
    public void MOV41_MOV44_SimpleAttackLimitIsAtLeastTarget()
    {
        var model = new SimpleAttack { Acceleration = 100f, PeakSpeed = 5f };

        Assert.Equal(C, model.Calculate(9f, 1, Dt, C));
        Assert.False(model.IsFinished(9f, C));
        Assert.True(model.IsFinished(C, C));
        Assert.Equal(-1f, new SimpleAttack { Acceleration = 10f, PeakSpeed = 15f }.Calculate(0f, -1, Dt, C), 3);
    }

    [Fact]
    public void MOV44_KeepSpeedPreservesSpeedAndFollowsInput()
    {
        var model = new KeepSpeed();

        Assert.Equal(-C, model.Calculate(C, -1, Dt, C));
        Assert.Equal(C, model.Calculate(C, 0, Dt, C));
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
