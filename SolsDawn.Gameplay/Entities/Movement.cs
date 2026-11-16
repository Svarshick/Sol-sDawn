using System;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay.Entities;

public struct MovementMemory
{
    public int Phase;
    public Sign PrevInDir;
}

public interface IMovementModel
{
    float Calculate(float velocity, Sign inDir, float dt, ref MovementMemory memory);
}

public class MovementCategoryAttribute(Type fallbackType) : CategoryAttribute(fallbackType)
{
    public override string GetTagsDescription() => "Movement";
}

public class MovementModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is MovementCategoryAttribute;

    public override string GetTagsDescription() => "Movement";
}
