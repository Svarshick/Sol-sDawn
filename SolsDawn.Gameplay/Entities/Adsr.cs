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
    float Calculate(float velocity, int inDir, float dt, float targetSpeed);
}

public interface IAdsrAttackModel : IAdsrModel
{
    bool IsFinished(float speed, float targetSpeed);
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
public class SimpleAttack : IAdsrAttackModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Acceleration = 10f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float PeakSpeed = 15f;

    private float Limit(float targetSpeed) => Math.Max(PeakSpeed, targetSpeed);

    public float Calculate(float velocity, int inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? inDir : Math.Sign(velocity);
        return dir * Math.Min(Limit(targetSpeed), Math.Abs(velocity) + Acceleration * dt);
    }

    public bool IsFinished(float speed, float targetSpeed) => speed >= Limit(targetSpeed);
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class LinearSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Deceleration = 10f;

    public float Calculate(float velocity, int inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, Math.Abs(velocity) - Deceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Sustain)]
public class KeepSpeed : IAdsrModel
{
    public float Calculate(float velocity, int inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? inDir : Math.Sign(velocity);
        return dir * Math.Abs(velocity);
    }
}

[MovementModel]
public class Adsr : IMovementModel
{
    private const float SpeedTolerance = 0.001f;

    [Inspect(0.5f, 0f, 100f)]
    public float ControlledSpeed = 10f;

    [AdsrCategory(AllowedAdsrState.Attack, typeof(SimpleAttack))]
    public CategorySlot<IAdsrAttackModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Decay, typeof(LinearSlowdown))]
    public CategorySlot<IAdsrModel> Decay = new();

    [AdsrCategory(AllowedAdsrState.Sustain, typeof(KeepSpeed))]
    public CategorySlot<IAdsrModel> Sustain = new();

    [AdsrCategory(AllowedAdsrState.Release, typeof(LinearSlowdown))]
    public CategorySlot<IAdsrModel> Release = new();

    public float Calculate(float velocity, int inDir, float dt, ref MovementMemory memory)
    {
        velocity = NormalizeVelocity(velocity);
        var state = SelectState((AdsrState)memory.Phase, Math.Abs(velocity), inDir);

        float result;
        switch (state)
        {
            case AdsrState.Attack:
                var attack = Attack.GetActiveInstance();
                result = attack.Calculate(velocity, inDir, dt, ControlledSpeed);
                var speed = Math.Abs(result);
                if (speed >= ControlledSpeed && attack.IsFinished(speed, ControlledSpeed))
                    state = AdsrState.Decay;
                break;
            case AdsrState.Decay:
                result = Decay.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed);
                break;
            case AdsrState.Sustain:
                result = Sustain.GetActiveInstance().Calculate(velocity, inDir, dt, ControlledSpeed);
                break;
            case AdsrState.Release:
                result = Release.GetActiveInstance().Calculate(velocity, inDir, dt, 0f);
                break;
            default:
                result = 0f;
                break;
        }

        memory.Phase = (int)state;
        return result;
    }

    private float NormalizeVelocity(float velocity)
    {
        var speed = Math.Abs(velocity);
        if (Math.Abs(speed - ControlledSpeed) <= SpeedTolerance)
            return Math.Sign(velocity) * ControlledSpeed;

        return speed <= SpeedTolerance ? 0f : velocity;
    }

    private AdsrState SelectState(AdsrState current, float speed, int inDir)
    {
        var held = inDir != 0;

        if (current == AdsrState.Attack && held)
            return AdsrState.Attack;
        if (speed > ControlledSpeed)
            return AdsrState.Decay;
        if (held)
            return speed == ControlledSpeed ? AdsrState.Sustain : AdsrState.Attack;

        return speed == 0f ? AdsrState.Idle : AdsrState.Release;
    }
}
