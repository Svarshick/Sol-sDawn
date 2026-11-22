using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal struct MovementMemory
{
    public int Phase;
}

internal interface IMovementModel
{
    float Calculate(float velocity, Sign inputDir, float dt, ref MovementMemory memory);
}

internal class MovementCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Movement";
}

internal class MovementModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is MovementCategoryAttribute;

    public override string GetTagsDescription() => "Movement";
}

internal class EnvelopeCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Envelope";
}

internal class EnvelopeModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is EnvelopeCategoryAttribute;

    public override string GetTagsDescription() => "Envelope";
}

internal class TurnCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Turn";
}

internal class TurnModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is TurnCategoryAttribute;

    public override string GetTagsDescription() => "Turn";
}

public interface IProjectionModel
{
    float Calculate(Vector2 velocity, Vector2 tangent);
}

internal class ProjectionCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Projection";
}

public class ProjectionModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is ProjectionCategoryAttribute;

    public override string GetTagsDescription() => "Projection";
}

[Flags]
public enum AllowedAimAxis
{
    Blade = 1 << 0,
    Fire  = 1 << 1
}

public interface IAimAxisModel
{
    (Vector2 Axis, float Speed) Calculate(Vector2 velocity, Vector2 aim, Vector2 tangent);
}

internal class AimAxisCategoryAttribute : CategoryAttribute
{
    public AllowedAimAxis State { get; }

    public AimAxisCategoryAttribute(AllowedAimAxis state)
    {
        State = state;
    }

    public override string GetTagsDescription() => $"Aim axis: {State}";
}

public class AimAxisModelAttribute : ModelAttribute
{
    public AllowedAimAxis States { get; }

    public AimAxisModelAttribute(AllowedAimAxis states)
    {
        States = states;
    }

    public override bool Satisfies(CategoryAttribute slot) =>
        slot is AimAxisCategoryAttribute constraint && (States & constraint.State) == constraint.State;

    public override int Breadth => System.Numerics.BitOperations.PopCount((uint)States);

    public override string GetTagsDescription() => $"Aim axis: {States}";
}

public interface IAxisModel
{
    (Vector2 Axis, float Speed) Calculate(Vector2 velocity);
}

internal class AxisCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Axis";
}

public class AxisModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is AxisCategoryAttribute;

    public override string GetTagsDescription() => "Axis";
}
