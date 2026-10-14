using System;
using System.Text.Json.Serialization;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay.Entities;

[Flags] public enum AllowedPhase { Drive = 1 << 0, Brake = 1 << 1, Turn  = 1 << 2, All = Drive | Brake | Turn }
[Flags] public enum AllowedSpeed { Normal = 1 << 0, GreatSpeed = 1 << 1, Any = Normal | GreatSpeed }
public enum MovePhase { Drive, Brake, Turn }

// JSON Polymorphism so Serialization/Deserialization knows exactly which implementations exist
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FrictionGreatSpeedDrive), nameof(FrictionGreatSpeedDrive))]
[JsonDerivedType(typeof(KeepGreatSpeedDrive), nameof(KeepGreatSpeedDrive))]
[JsonDerivedType(typeof(FrictionGreatSpeedDecay), nameof(FrictionGreatSpeedDecay))]
[JsonDerivedType(typeof(LinearDriveModel), nameof(LinearDriveModel))]
[JsonDerivedType(typeof(LinearBrakeModel), nameof(LinearBrakeModel))]
[JsonDerivedType(typeof(LinearTurnModel), nameof(LinearTurnModel))]
public interface IMotionModel
{
    float Calculate(float velocity, int inDir, float dt, float controlledSpeed);
}

public class MotionModelAttribute : ModelAttribute
{
    public AllowedPhase Phase { get; }
    public AllowedSpeed Speed { get; }

    public MotionModelAttribute(AllowedPhase phase, AllowedSpeed speed)
    {
        Phase = phase;
        Speed = speed;
    }

    public override bool Satisfies(CategoryAttribute slot)
    {
        if (slot is not MotionCategoryAttribute constraint) return false;
        bool phaseMatches = (Phase & constraint.Phases) == constraint.Phases;
        bool speedMatches = (Speed == AllowedSpeed.Any || constraint.Speed == AllowedSpeed.Any || Speed == constraint.Speed);
        return phaseMatches && speedMatches;
    }
}

public class MotionCategoryAttribute : CategoryAttribute
{
    public AllowedPhase Phases { get; }
    public AllowedSpeed Speed { get; }

    public MotionCategoryAttribute(AllowedPhase phases, AllowedSpeed speed, Type fallbackType) 
        : base(fallbackType)
    {
        Phases = phases;
        Speed = speed;
    }
}

// --- IMPLEMENTATIONS ---

[MotionModel(AllowedPhase.Drive, AllowedSpeed.GreatSpeed)]
public class FrictionGreatSpeedDrive : IMotionModel
{
    [Inspect(1f, 0f, 200f)] public float Friction { get; set; } = 10f;
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed)
    {
        var speed = Math.Max(controlledSpeed, Math.Abs(velocity) - Friction * dt);
        return inDir * speed;
    }
}

[MotionModel(AllowedPhase.Drive, AllowedSpeed.GreatSpeed)]
public class KeepGreatSpeedDrive : IMotionModel
{
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed) => velocity;
}

[MotionModel(AllowedPhase.Brake | AllowedPhase.Turn, AllowedSpeed.GreatSpeed)]
public class FrictionGreatSpeedDecay : IMotionModel
{
    [Inspect(1f, 0f, 200f)] public float Deceleration { get; set; } = 20f;
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed)
    {
        var speed = Math.Max(controlledSpeed, Math.Abs(velocity) - Deceleration * dt);
        return Math.Sign(velocity) * speed;
    }
}

[MotionModel(AllowedPhase.Drive, AllowedSpeed.Normal)]
public class LinearDriveModel : IMotionModel
{
    [Inspect(1f, 0f, 200f)] public float Acceleration { get; set; } = 15f;
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed)
    {
        var speed = Math.Min(controlledSpeed, Math.Abs(velocity) + Acceleration * dt);
        return inDir * speed;
    }
}

[MotionModel(AllowedPhase.Brake, AllowedSpeed.Normal)]
public class LinearBrakeModel : IMotionModel
{
    [Inspect(1f, 0f, 200f)] public float Friction { get; set; } = 15f;
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed)
    {
        var speed = Math.Max(0f, Math.Abs(velocity) - Friction * dt);
        return Math.Sign(velocity) * speed;
    }
}

[MotionModel(AllowedPhase.Turn, AllowedSpeed.Normal)]
public class LinearTurnModel : IMotionModel
{
    [Inspect(1f, 0f, 200f)] public float TurnAcceleration { get; set; } = 30f;
    public float Calculate(float velocity, int inDir, float dt, float controlledSpeed)
    {
        var nextVelocity = velocity + inDir * TurnAcceleration * dt;
        if (Math.Sign(nextVelocity) == inDir && Math.Abs(nextVelocity) >= controlledSpeed)
            return inDir * controlledSpeed;
        return nextVelocity;
    }
}

// --- THE SOLVER ---

public class MotionSolver
{
    [Inspect(0.5f, 0f, 100f)] public float ControlledSpeed { get; set; } = 15f;

    [MotionCategory(AllowedPhase.Drive, AllowedSpeed.GreatSpeed, typeof(FrictionGreatSpeedDrive))] 
    public CategorySlot<IMotionModel> GreatSpeedDrive = new();
    
    [MotionCategory(AllowedPhase.Brake, AllowedSpeed.GreatSpeed, typeof(FrictionGreatSpeedDecay))] 
    public CategorySlot<IMotionModel> GreatSpeedBrake = new();
    
    [MotionCategory(AllowedPhase.Turn, AllowedSpeed.GreatSpeed, typeof(FrictionGreatSpeedDecay))] 
    public CategorySlot<IMotionModel> GreatSpeedTurn = new();
    
    [MotionCategory(AllowedPhase.Drive, AllowedSpeed.Normal, typeof(LinearDriveModel))] 
    public CategorySlot<IMotionModel> NormalDrive = new();
    
    [MotionCategory(AllowedPhase.Brake, AllowedSpeed.Normal, typeof(LinearBrakeModel))] 
    public CategorySlot<IMotionModel> NormalBrake = new();
    
    [MotionCategory(AllowedPhase.Turn, AllowedSpeed.Normal, typeof(LinearTurnModel))] 
    public CategorySlot<IMotionModel> NormalTurn = new();

    [JsonIgnore] public MovePhase Phase { get; private set; }

    public MotionSolver()
    {
        // Setup initial default models dynamically based on attributes for safety
        ConfigurationUtility.InitializeConfig(this);
    }

    public void ResetPhase(MovePhase phase = MovePhase.Drive) => Phase = phase;

    public float Calculate(float velocity, int inDir, float dt)
    {
        var speed = Math.Abs(velocity);
        var velDir = Math.Sign(velocity);
        var isGreatSpeed = speed > ControlledSpeed;
        
        Phase = inDir switch
        {
            0 => MovePhase.Brake,
            _ when inDir == velDir && speed >= ControlledSpeed => MovePhase.Drive,
            _ when inDir == -velDir || Phase == MovePhase.Turn => MovePhase.Turn,
            _ => MovePhase.Drive
        };

        return (isGreatSpeed, Phase) switch
        {
            (true, MovePhase.Drive) => GreatSpeedDrive.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            (true, MovePhase.Brake) => GreatSpeedBrake.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            (true, MovePhase.Turn)  => GreatSpeedTurn.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            (false, MovePhase.Drive) => NormalDrive.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            (false, MovePhase.Brake) => NormalBrake.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            (false, MovePhase.Turn)  => NormalTurn.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed),
            _ => throw Log.Logic.FatalException()
        };
    }
}