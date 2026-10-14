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
    
    public PlayerBoard()
    {
        // Initializes all nested CategorySlots inside all nested MotionSolvers
        ConfigurationUtility.InitializeConfig(Specs);
    }
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
        [Inspect] public MotionSolver MotionSolver = new();
    }

    public PrimitiveJumpSpecs PrimitiveJump = new();
    public record PrimitiveJumpSpecs
    {
        [Inspect] public MotionSolver HorizontalMotionSolver = new();
        [Inspect(min: 0)] public float VerticalStartSpeed = 20;
        [Inspect(min: 0)] public float VerticalFriction = 15;
    }

    public CelesteJumpSpecs CelesteJump = new();
    public record CelesteJumpSpecs
    {
        [Inspect] public MotionSolver HorizontalMotionSolver = new();
        
        [Inspect(min: 0, max: PI / 2)] public float HorizontalDashAngle = PI / 6;
        [Inspect(min: 0)] public float HorizontalDashDistance = 5;
        [Inspect(min: 0)] public float VerticalDashDistance = 5;
        [Inspect(min: 1)] public int DashFrames = 3;

        [Inspect(min: 0)] public float HorizontalDashFinalSpeed = 20;
        [Inspect(min: 0)] public float VerticalDashFinalSpeed = 20;
        
        [Inspect(min: 0)] public float VerticalFriction = 15;
    }

    public FallSpecs Fall = new();
    public record FallSpecs
    {
        [Inspect] public MotionSolver HorizontalMotionSolver = new();
        
        [Inspect(min: 0)] public float VerticalAcceleration = 15;
        [Inspect(min: 0)] public float VerticalMaxSpeed = 30;
    }
}