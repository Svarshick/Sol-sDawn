using System.Diagnostics.CodeAnalysis;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class Player : Core.Logic.Gameplay.Entity<PlayerBoard, PlayerAnimations>
{
    public Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, null, board, animation, Shapes.Circle(board.Specs.GetActiveInstance().ColliderRadius), Layer.None, Layer.None)
    {
    }
}

public class PlayerCategoryAttribute() : CategoryAttribute(typeof(PlayerSpecs))
{
    public override string GetTagsDescription() => "Entity: Player";
}

public class PlayerModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is PlayerCategoryAttribute;

    public override string GetTagsDescription() => "Entity: Player";
}

public record PlayerBoard
{
    [PlayerCategory]
    public CategorySlot<PlayerSpecs> Specs = new();

    [MemberNotNullWhen(true, nameof(CurrentPlatform))]
    public bool OnGround => CurrentPlatform is not null;
    public Platform? CurrentPlatform;
    public Vector2 Velocity;
}

[PlayerModel]
public record PlayerSpecs
{
    [Inspect] public Color Color = Color.Blue;
    [Inspect(0.01f, 0.05f, 5f)] public float ColliderRadius = 0.35f;
    [Inspect(0.05f, 0.1f, 10f)] public float Width = 0.7f;
    [Inspect(0.05f, 0.1f, 10f)] public float Height = 1.3f;

    [StateCategory(AllowedState.Idle, typeof(IdleState))]
    public CategorySlot<PlayerState> Idle = new();

    [StateCategory(AllowedState.Run, typeof(RunState))]
    public CategorySlot<PlayerState> Run = new();

    [StateCategory(AllowedState.Jump, typeof(CelesteJumpState))]
    public CategorySlot<PlayerState> Jump = new();

    [StateCategory(AllowedState.Fall, typeof(FallState))]
    public CategorySlot<PlayerState> Fall = new();
}