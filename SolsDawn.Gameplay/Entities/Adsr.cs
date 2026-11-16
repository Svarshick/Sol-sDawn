using System;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay.Entities;

[Flags]
public enum AllowedAdsrState
{
    Attack  = 1 << 0,
    Decay   = 1 << 1,
    Sustain = 1 << 2,
    Release = 1 << 3
}

public enum AdsrState
{
    Idle,
    Attack,
    Decay,
    Sustain,
    Release
}

public interface IAdsrModel
{
    float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed);
}

public class AdsrCategoryAttribute : CategoryAttribute
{
    public AllowedAdsrState State { get; }

    public AdsrCategoryAttribute(AllowedAdsrState state, Type fallbackType)
        : base(fallbackType)
    {
        State = state;
    }

    public override string GetTagsDescription() => $"ADSR: {State}";
}

public class AdsrModelAttribute : ModelAttribute
{
    public AllowedAdsrState States { get; }

    public AdsrModelAttribute(AllowedAdsrState states)
    {
        States = states;
    }

    public override bool Satisfies(CategoryAttribute baseConstraint)
    {
        if (baseConstraint is not AdsrCategoryAttribute constraint)
            return false;

        return (States & constraint.State) == constraint.State;
    }

    public override string GetTagsDescription() => $"ADSR: {States}";
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SimpleAttack : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Acceleration = 10f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, Math.Abs(velocity) + Acceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class LinearSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Deceleration = 10f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, Math.Abs(velocity) - Deceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Sustain)]
public class KeepSpeed : IAdsrModel
{
    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Abs(velocity);
    }
}

[MovementModel]
public class Adsr : IMovementModel
{
    private const float SpeedTolerance = 0.001f;

    [Inspect(0.5f, 0f, 100f)]
    public float ControlledSpeed = 10f;

    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack, typeof(SimpleAttack))]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Decay, typeof(LinearSlowdown))]
    public CategorySlot<IAdsrModel> Decay = new();

    [AdsrCategory(AllowedAdsrState.Sustain, typeof(KeepSpeed))]
    public CategorySlot<IAdsrModel> Sustain = new();

    [AdsrCategory(AllowedAdsrState.Release, typeof(LinearSlowdown))]
    public CategorySlot<IAdsrModel> Release = new();

    public float Calculate(float velocity, Sign inDir, float dt, ref MovementMemory memory)
    {
        velocity = NormalizeVelocity(velocity);
        var limit = Math.Max(PeakSpeed, ControlledSpeed);
        var previous = (AdsrState)memory.Phase;
        var state = SelectState(previous, Math.Abs(velocity), inDir, limit);
        var prevInDir = state == previous ? memory.PrevInDir : inDir;

        var result = state switch
        {
            AdsrState.Attack => Attack.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, limit),
            AdsrState.Decay => Decay.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, ControlledSpeed),
            AdsrState.Sustain => Sustain.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, ControlledSpeed),
            AdsrState.Release => Release.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, 0f),
            _ => 0f
        };

        memory.Phase = (int)state;
        memory.PrevInDir = inDir;
        return result;
    }

    private float NormalizeVelocity(float velocity)
    {
        var speed = Math.Abs(velocity);
        if (Math.Abs(speed - ControlledSpeed) <= SpeedTolerance)
            return Math.Sign(velocity) * ControlledSpeed;

        return speed <= SpeedTolerance ? 0f : velocity;
    }

    private AdsrState SelectState(AdsrState previous, float speed, Sign inDir, float limit)
    {
        var held = inDir != 0;

        if (previous == AdsrState.Attack && held && speed < limit)
            return AdsrState.Attack;
        if (speed > ControlledSpeed)
            return AdsrState.Decay;
        if (held)
            return speed == ControlledSpeed ? AdsrState.Sustain : AdsrState.Attack;

        return speed == 0f ? AdsrState.Idle : AdsrState.Release;
    }
}

public enum EnvelopeShape
{
    AR,
    AS
}

public class EnvelopeCategoryAttribute : CategoryAttribute
{
    public EnvelopeShape Shape { get; }

    public EnvelopeCategoryAttribute(EnvelopeShape shape, Type fallbackType)
        : base(fallbackType)
    {
        Shape = shape;
    }

    public override string GetTagsDescription() => $"Envelope {Shape}";
}

public class EnvelopeModelAttribute : ModelAttribute
{
    public EnvelopeShape Shape { get; }

    public EnvelopeModelAttribute(EnvelopeShape shape)
    {
        Shape = shape;
    }

    public override bool Satisfies(CategoryAttribute baseConstraint) =>
        baseConstraint is EnvelopeCategoryAttribute constraint && constraint.Shape == Shape;

    public override string GetTagsDescription() => $"Envelope {Shape}";
}

public enum ArPhase
{
    Start,
    Attack,
    Release,
    Done
}

[EnvelopeModel(EnvelopeShape.AR)]
public class AR : IMovementModel
{
    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack, typeof(SimpleAttack))]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Release, typeof(LinearSlowdown))]
    public CategorySlot<IAdsrModel> Release = new();

    public float Calculate(float velocity, Sign inDir, float dt, ref MovementMemory memory)
    {
        var previous = (ArPhase)memory.Phase;
        var phase = previous switch
        {
            ArPhase.Start or ArPhase.Attack when Math.Abs(velocity) < PeakSpeed => ArPhase.Attack,
            ArPhase.Start or ArPhase.Attack or ArPhase.Release => ArPhase.Release,
            _ => ArPhase.Done
        };
        var prevInDir = phase == previous ? memory.PrevInDir : inDir;

        var result = 0f;
        if (phase == ArPhase.Attack)
        {
            result = Attack.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, PeakSpeed);
        }
        else if (phase == ArPhase.Release)
        {
            result = Release.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, 0f);
            if (result == 0f)
                phase = ArPhase.Done;
        }

        memory.Phase = (int)phase;
        memory.PrevInDir = inDir;
        return result;
    }

    public static bool IsDone(in MovementMemory memory) => (ArPhase)memory.Phase == ArPhase.Done;
}

public enum AsPhase
{
    Start,
    Attack,
    Sustain
}

[EnvelopeModel(EnvelopeShape.AS)]
public class AS : IMovementModel
{
    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack, typeof(SimpleAttack))]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Sustain, typeof(KeepSpeed))]
    public CategorySlot<IAdsrModel> Sustain = new();

    public float Calculate(float velocity, Sign inDir, float dt, ref MovementMemory memory)
    {
        var previous = (AsPhase)memory.Phase;
        var phase = previous is AsPhase.Start or AsPhase.Attack && Math.Abs(velocity) < PeakSpeed
            ? AsPhase.Attack
            : AsPhase.Sustain;
        var prevInDir = phase == previous ? memory.PrevInDir : inDir;

        var result = phase == AsPhase.Attack
            ? Attack.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, PeakSpeed)
            : Sustain.GetActiveInstance().Calculate(velocity, prevInDir, inDir, dt, PeakSpeed);

        memory.Phase = (int)phase;
        memory.PrevInDir = inDir;
        return result;
    }
}
