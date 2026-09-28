using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class Player : Entity<PlayerBoard, PlayerAnimations>
{
    public Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, board, animation, Shapes.Circle(board.Specs.Width/2), Layer.Player, Layer.Wall)
    {
        Collider.OnCollision += data =>
        {
            if (data.Other.GameObject.TryGetComponent<Platform>(out var platform))
            {
                var tangent = Vector2.Normalize(platform.Point2 - platform.Point1);
                var normal = new Vector2(-tangent.Y, tangent.X);
                if (Vector2.Dot(normal, Collider._body.LinearVelocity) >= 0)
                    return false;
                    
                Board.TouchedPlatforms.Add(platform);
                if (!Board.OnGround)
                {
                    Board.CurrentPlatform = platform;
                }
            }
            return true;
        };

        Collider.OnSeparation += data =>
        {
            if (data.Other.GameObject.TryGetComponent<Platform>(out var platform))
            {
                Board.TouchedPlatforms.Remove(platform);
                //NOTE: It would work well with 2 touched platform but 3+ could have edge cases, maybe...
                Board.CurrentPlatform = Board.TouchedPlatforms.Count != 0 ? Board.TouchedPlatforms[0] : null;
            }
        };
    }
}

public record PlayerBoard
{
    public PlayerSpecs Specs = new();

    public bool OnGround => CurrentPlatform is not null;
    public readonly List<Platform> TouchedPlatforms = new();
    public Platform? CurrentPlatform;
}

public record PlayerSpecs
{
    public Color Color = Color.Blue;
    public float Width = 0.7f;
    public float Height = 1.3f;

    public RunSpecs Run = new();
    public record RunSpecs
    {
        [Inspect(min: 0)] public float GreatSpeedFriction = 20;
        [Inspect(min: 0)] public float Friction = 30;
        [Inspect(min: 0)] public float Acceleration = 10;
        [Inspect(min: 0)] public float TurnAcceleration = 30;
        [Inspect(min: 0)] public float Speed = 15;
    }

    public PrimitiveJumpSpecs PrimitiveJump = new();
    public record PrimitiveJumpSpecs
    {
        [Inspect(min: 0)] public float HorizontalStartSpeed = 20;
        [Inspect(min: 0)] public float VerticalStartSpeed = 20;
        [Inspect(min: 0)] public float HorizontalSpeed = 15;
        [Inspect(min: 0)] public float VerticalAcceleration = -15;
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
        [Inspect(min: 0)] public float FallHorizontalSpeed = 10;
        [Inspect(min: 0)] public float FallVerticalAcceleration = 15;
        [Inspect(min: 0)] public float FallVerticalSpeedLimit = 30;
    }
}