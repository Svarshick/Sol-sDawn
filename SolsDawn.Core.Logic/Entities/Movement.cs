using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Entities;

public struct MovementMemory
{
    public int Phase;
}

public interface IMovementModel
{
    float Calculate(float velocity, Sign inputDir, float dt, ref MovementMemory memory);
}

public class MovementCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Movement";
}

public class MovementModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is MovementCategoryAttribute;

    public override string GetTagsDescription() => "Movement";
}

public class EnvelopeCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Envelope";
}

public class EnvelopeModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is EnvelopeCategoryAttribute;

    public override string GetTagsDescription() => "Envelope";
}

public class TurnCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Turn";
}

public class TurnModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is TurnCategoryAttribute;

    public override string GetTagsDescription() => "Turn";
}

public interface IProjectionModel
{
    float Calculate(Vector2 velocity, Vector2 tangent);
}

public class ProjectionCategoryAttribute : CategoryAttribute
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

public class AimAxisCategoryAttribute : CategoryAttribute
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

public interface IBounceAxisModel
{
    (Vector2 Axis, float Speed) Calculate(Vector2 velocity);
}

public class BounceAxisCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Bounce axis";
}

public class BounceAxisModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is BounceAxisCategoryAttribute;

    public override string GetTagsDescription() => "Bounce axis";
}
