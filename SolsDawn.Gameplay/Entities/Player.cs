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

    public RunSpecs Run = new();
    public record RunSpecs
    {
        [Inspect(min: 0)] public float GreatSpeedFriction = 20;
        [Inspect(min: 0)] public float Friction = 30;
        [Inspect(min: 0)] public float Acceleration = 10;
        [Inspect(min: 0)] public float TurnAcceleration = 30;
        [Inspect(min: 0)] public float MaxSpeed = 15;
    }

    public PrimitiveJumpSpecs PrimitiveJump = new();
    public record PrimitiveJumpSpecs
    {
        [Inspect(min: 0)] public float HorizontalStartSpeed = 20;
        [Inspect(min: 0)] public float VerticalStartSpeed = 20;
        [Inspect(min: 0)] public float HorizontalSpeed = 15;
        [Inspect(min: 0)] public float VerticalAcceleration = 15;
    }

    public CelesteJumpSpecs CelesteJump = new();
    public record CelesteJumpSpecs
    {
        [Inspect(min: 0)] public float HorizontalDashAngle = 30;
        [Inspect(min: 0)] public float HorizontalDashDistance = 10;
        [Inspect(min: 0)] public float VerticalDashDistance = 10;
        [Inspect(min: 1)] public int DashFrames = 3;
        [Inspect(min: 0)] public int PeakReachedFrames;
        [Inspect(min: 0)] public float PeakVerticalDelta;
        [Inspect(min: 0)] public float PeakHorizontalSpeed;
        [Inspect(min: 0)] public float PeakHorizontalBrakeAcceleration; //in friction instead of frames because Dash -> stop is slower than Peak -> stop
    }

    public FallSpecs Fall = new();
    public record FallSpecs
    {
        [Inspect(min: 0)] public float HorizontalGreatSpeedFriction = 20;
        [Inspect(min: 0)] public float HorizontalFriction = 10;
        [Inspect(min: 0)] public float HorizontalAcceleration = 10;
        [Inspect(min: 0)] public float HorizontalTurnAcceleration = 30;
        [Inspect(min: 0)] public float HorizontalMaxSpeed = 20;
        [Inspect(min: 0)] public float VerticalAcceleration = 15;
        [Inspect(min: 0)] public float VerticalMaxSpeed = 30;
    }
}