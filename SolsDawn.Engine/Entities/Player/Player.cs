using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using static SolsDawn.Engine.GameplayAPI;

namespace SolsDawn.Engine;

public class Player : Entity<PlayerBoard, PlayerAnimations>
{
    public readonly Hurtbox Hurtbox;
    internal readonly PlayerController Controller;

    internal Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, null, board, animation, Shapes.Circle(board.Specs.GetActiveInstance().ColliderRadius), Layer.Player)
    {
        var hurtboxObject = CreateObject();
        hurtboxObject.Transform.Parent = go.Transform;
        Hurtbox = new Hurtbox(hurtboxObject, Shapes.Circle(board.Specs.GetActiveInstance().ColliderRadius), Side.Player);
        Hurtbox.Hit.OnFire(() => Animator.Player.TryPlay(AnimationSet.Hit));
        Controller = new PlayerController(this);
    }

    public override void OnDestroyImmediate()
    {
        base.OnDestroyImmediate();
        if (G.Player == this)
            G.Player = null;
    }
}

public class PlayerCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Entity: Player";
}

internal class PlayerModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is PlayerCategoryAttribute;

    public override string GetTagsDescription() => "Entity: Player";
}

public record PlayerBoard
{
    public required CategorySlot<PlayerSpecs> Specs;

    [MemberNotNullWhen(true, nameof(CurrentPlatform))]
    public bool OnGround => CurrentPlatform is not null;
    public Platform? CurrentPlatform;
    public Vector2 Velocity;
    public Vector2 AttackPointer;
    public bool ControlBack;
    public bool Parried;
}

[PlayerModel]
public record PlayerSpecs
{
    [Inspect] public Color Color = Color.Blue;
    [Inspect(0.01f, 0.05f, 5f)] public float ColliderRadius = 0.35f;
    [Inspect(0.01f, 0f, 5f)] public float HitDuration = 0.2f;
    [Inspect] public Color HitBlinkColor = Color.White;

    [StateCategory(AllowedState.Idle)]
    public CategorySlot<PlayerState> Idle = new();

    [StateCategory(AllowedState.Run)]
    public CategorySlot<PlayerState> Run = new();

    [StateCategory(AllowedState.Jump)]
    public CategorySlot<PlayerState> Jump = new();

    [StateCategory(AllowedState.Fall)]
    public CategorySlot<PlayerState> Fall = new();

    [StateCategory(AllowedState.Blade)]
    public CategorySlot<PlayerState> Blade = new();

    [StateCategory(AllowedState.Fire)]
    public CategorySlot<PlayerState> Fire = new();

    [StateCategory(AllowedState.DownAttack)]
    public CategorySlot<PlayerState> DownAttack = new();

    [StateCategory(AllowedState.Bounce)]
    public CategorySlot<PlayerState> Bounce = new();
}