using System.Diagnostics.CodeAnalysis;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class Player : Core.Logic.Gameplay.Entity<PlayerBoard, PlayerAnimations>
{
    public Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, null, board, animation, Shapes.Circle(board.Specs.ColliderRadius), Layer.None, Layer.None)
    {
    }
}

public record PlayerBoard
{
    public PlayerSpecs Specs = new();

    [MemberNotNullWhen(true, nameof(CurrentPlatform))]
    public bool OnGround => CurrentPlatform is not null;
    public Platform? CurrentPlatform;
    public Vector2 Velocity;
}

public record PlayerSpecs
{
    public Color Color = Color.Blue;
    public float ColliderRadius = 0.35f;
    public float Width = 0.7f;
    public float Height = 1.3f;

    [StateCategory(AllowedState.Idle, typeof(IdleState))]
    public CategorySlot<PlayerState> Idle = new();

    [StateCategory(AllowedState.Run, typeof(RunState))]
    public CategorySlot<PlayerState> Run = new();

    [StateCategory(AllowedState.Jump, typeof(CelesteJumpState))]
    public CategorySlot<PlayerState> Jump = new();

    [StateCategory(AllowedState.Fall, typeof(FallState))]
    public CategorySlot<PlayerState> Fall = new();
}