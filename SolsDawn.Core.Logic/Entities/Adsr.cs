using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Entities;

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

    public AdsrCategoryAttribute(AllowedAdsrState state)
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

    public override int Breadth => System.Numerics.BitOperations.PopCount((uint)States);

    public override string GetTagsDescription() => $"ADSR: {States}";
}

[MovementModel]
public class Adsr : IMovementModel
{
    private const float SpeedTolerance = 0.001f;

    [Inspect(0.5f, 0f, 100f)]
    public float ControlledSpeed = 10f;

    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Decay)]
    public CategorySlot<IAdsrModel> Decay = new();

    [AdsrCategory(AllowedAdsrState.Sustain)]
    public CategorySlot<IAdsrModel> Sustain = new();

    [AdsrCategory(AllowedAdsrState.Release)]
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
    AS
}

public class EnvelopeCategoryAttribute : CategoryAttribute
{
    public EnvelopeShape Shape { get; }

    public EnvelopeCategoryAttribute(EnvelopeShape shape)
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

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Sustain)]
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
