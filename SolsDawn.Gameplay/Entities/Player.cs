using System;
using System.Collections.Generic;
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
    
    public float RunSpeed = 15;

    public float JumpHorizontalStartSpeed = 20;
    public float JumpVerticalStartSpeed = 15;
    public float JumpHorizontalSpeed = 10;
    public float JumpVerticalAcceleration = -10;
    
    public float FallHorizontalSpeed = 10;
    public float FallVerticalAcceleration = -15;
    public float FallVerticalSpeedLimit = -30;
}