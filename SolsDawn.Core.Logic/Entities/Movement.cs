using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Entities;

public struct MovementMemory
{
    public int Phase;
    public Sign PrevInDir;
}

public interface IMovementModel
{
    float Calculate(float velocity, Sign inDir, float dt, ref MovementMemory memory);
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
