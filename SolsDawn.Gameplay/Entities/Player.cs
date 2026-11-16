using System.Diagnostics.CodeAnalysis;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Combat;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class Player : Core.Logic.Gameplay.Entity<PlayerBoard, PlayerAnimations>
{
    public readonly Hurtbox Hurtbox;

    public Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, null, board, animation, Shapes.Circle(board.Specs.GetActiveInstance().ColliderRadius), Layer.Player)
    {
        var hurtboxObject = CreateObject();
        hurtboxObject.Transform.Parent = go.Transform;
        Hurtbox = new Hurtbox(hurtboxObject, Shapes.Circle(board.Specs.GetActiveInstance().ColliderRadius), Side.Player);
        Hurtbox.Hit.OnFire(() => Animator.Player.TryPlay(AnimationSet.Hit));
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

    [StateCategory(AllowedState.Idle, typeof(IdleState))]
    public CategorySlot<PlayerState> Idle = new();

    [StateCategory(AllowedState.Run, typeof(RunState))]
    public CategorySlot<PlayerState> Run = new();

    [StateCategory(AllowedState.Jump, typeof(CelesteJumpState))]
    public CategorySlot<PlayerState> Jump = new();

    [StateCategory(AllowedState.Fall, typeof(FallState))]
    public CategorySlot<PlayerState> Fall = new();

    [StateCategory(AllowedState.Blade, typeof(SlideBlade))]
    public CategorySlot<PlayerState> Blade = new();

    [StateCategory(AllowedState.Fire, typeof(SimpleFire))]
    public CategorySlot<PlayerState> Fire = new();

    [StateCategory(AllowedState.DownAttack, typeof(SimpleDownAttack))]
    public CategorySlot<PlayerState> DownAttack = new();

    [StateCategory(AllowedState.Bounce, typeof(BounceState))]
    public CategorySlot<PlayerState> Bounce = new();
}