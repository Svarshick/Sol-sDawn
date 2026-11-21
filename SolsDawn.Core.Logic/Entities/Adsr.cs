using System;

namespace SolsDawn.Core.Logic.Entities;

[Flags]
public enum AllowedAdsrState
{
    Attack  = 1 << 0,
    Decay   = 1 << 1,
    Release = 1 << 2
}

public enum AdsrPhase
{
    Idle,
    Attack,
    Decay,
    Sustain,
    Release,
    TurnDecay,
    TurnRelease,
    TurnAttack
}

public interface IAdsrModel
{
    float Calculate(float dt, float speed, float fromSpeed, float toSpeed);
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

internal static class AdsrMath
{
    private const float SpeedTolerance = 0.001f;

    public static float Normalize(float speed, float controlledSpeed)
    {
        if (Math.Abs(speed - controlledSpeed) <= SpeedTolerance)
            return controlledSpeed;

        return speed <= SpeedTolerance ? 0f : speed;
    }

    public static AdsrPhase Select(AdsrPhase previous, float speed, Sign motion, Sign inputDir, float controlledSpeed, float peakSpeed)
    {
        var none = inputDir == 0;
        var against = !none && speed > 0f && inputDir == -motion;
        var along = !none && !against;

        if (previous == AdsrPhase.TurnAttack && along && speed < peakSpeed)
            return AdsrPhase.TurnAttack;
        if (against)
            return speed > controlledSpeed ? AdsrPhase.TurnDecay : AdsrPhase.TurnRelease;
        if (previous is AdsrPhase.TurnDecay or AdsrPhase.TurnRelease && along && speed == 0f)
            return AdsrPhase.TurnAttack;
        if (previous == AdsrPhase.Attack && along && speed < peakSpeed)
            return AdsrPhase.Attack;
        if (speed > controlledSpeed)
            return AdsrPhase.Decay;
        if (along)
            return speed == controlledSpeed ? AdsrPhase.Sustain : AdsrPhase.Attack;

        return speed == 0f ? AdsrPhase.Idle : AdsrPhase.Release;
    }

    public static float Turn(Dra turn, AdsrPhase phase, float dt, float speed, float controlledSpeed, float peakSpeed) => phase switch
    {
        AdsrPhase.TurnDecay => turn.Decay.GetActiveInstance().Calculate(dt, speed, peakSpeed, controlledSpeed),
        AdsrPhase.TurnRelease => turn.Release.GetActiveInstance().Calculate(dt, speed, controlledSpeed, 0f),
        _ => turn.Attack.GetActiveInstance().Calculate(dt, speed, 0f, peakSpeed)
    };

    public static float Direction(float speed, Sign motion, Sign inputDir) => speed > 0f ? motion : inputDir;
}

[TurnModel]
public class Dra
{
    [AdsrCategory(AllowedAdsrState.Decay)]
    public CategorySlot<IAdsrModel> Decay = new();

    [AdsrCategory(AllowedAdsrState.Release)]
    public CategorySlot<IAdsrModel> Release = new();

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Attack = new();
}

[MovementModel]
public class Adsr : IMovementModel, IConstrainedModel
{
    [Inspect(0.5f, 0f, 100f)]
    public float ControlledSpeed = 10f;

    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Decay)]
    public CategorySlot<IAdsrModel> Decay = new();

    [AdsrCategory(AllowedAdsrState.Release)]
    public CategorySlot<IAdsrModel> Release = new();

    [TurnCategory]
    public CategorySlot<Dra> Turn = new();

    public float Calculate(float velocity, Sign inputDir, float dt, ref MovementMemory memory)
    {
        var c = ControlledSpeed;
        var p = PeakSpeed;
        var speed = AdsrMath.Normalize(Math.Abs(velocity), c);
        Sign motion = velocity;
        var phase = AdsrMath.Select((AdsrPhase)memory.Phase, speed, motion, inputDir, c, p);

        var result = phase switch
        {
            AdsrPhase.Attack => Attack.GetActiveInstance().Calculate(dt, speed, 0f, p),
            AdsrPhase.Decay => Decay.GetActiveInstance().Calculate(dt, speed, p, c),
            AdsrPhase.Sustain => speed,
            AdsrPhase.Release => Release.GetActiveInstance().Calculate(dt, speed, c, 0f),
            AdsrPhase.TurnDecay or AdsrPhase.TurnRelease or AdsrPhase.TurnAttack =>
                AdsrMath.Turn(Turn.GetActiveInstance(), phase, dt, speed, c, p),
            _ => 0f
        };

        memory.Phase = (int)phase;
        return AdsrMath.Direction(speed, motion, inputDir) * result;
    }

    public void Constrain(string? editedField)
    {
        if (PeakSpeed >= ControlledSpeed)
            return;

        if (editedField == nameof(PeakSpeed))
            ControlledSpeed = PeakSpeed;
        else
            PeakSpeed = ControlledSpeed;
    }
}

[EnvelopeModel]
public class Ads : IMovementModel
{
    [Inspect(0.5f, 0f, 200f)]
    public float PeakSpeed = 15f;

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Attack = new();

    [AdsrCategory(AllowedAdsrState.Decay)]
    public CategorySlot<IAdsrModel> Decay = new();

    [TurnCategory]
    public CategorySlot<Dra> Turn = new();

    public float Calculate(float velocity, Sign inputDir, float dt, ref MovementMemory memory)
    {
        var p = PeakSpeed;
        var speed = AdsrMath.Normalize(Math.Abs(velocity), p);
        Sign motion = velocity;
        if (inputDir == 0)
            inputDir = motion;
        var phase = AdsrMath.Select((AdsrPhase)memory.Phase, speed, motion, inputDir, p, p);

        var result = phase switch
        {
            AdsrPhase.Attack => Attack.GetActiveInstance().Calculate(dt, speed, 0f, p),
            AdsrPhase.Decay => Decay.GetActiveInstance().Calculate(dt, speed, p, p),
            AdsrPhase.Sustain => speed,
            AdsrPhase.TurnDecay or AdsrPhase.TurnRelease or AdsrPhase.TurnAttack =>
                AdsrMath.Turn(Turn.GetActiveInstance(), phase, dt, speed, p, p),
            _ => 0f
        };

        memory.Phase = (int)phase;
        return AdsrMath.Direction(speed, motion, inputDir) * result;
    }
}
