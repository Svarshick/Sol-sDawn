using System;
using System.Diagnostics.CodeAnalysis;
using SolsDawn.Core;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class Player : Core.Logic.Gameplay.Entity<PlayerBoard, PlayerAnimations>
{
    private bool _platformChanged;
    
    public Player(GameObject go, PlayerBoard board, PlayerAnimations animation)
        : base(go, board, animation, Shapes.Circle(board.Specs.ColliderRadius), Layer.Player, Layer.Wall)
    {
        //Collider._body.FixtureList[0].Friction = 0f;
        //Collider._body.FixtureList[0].Restitution = 0f;

        Collider.OnCollision += data =>
        {
            if (!data.Other.GameObject.TryGetComponent<Platform>(out var platform))
                return true;

            if (_platformChanged)
            {
                Console.WriteLine($"{Time.TotalGameTime} Platform changed");
                return false;
            } 

            if (Board.CurrentPlatform == platform)
                throw new LogicException("Touched the same platform twice");

            var tangent = Vector2.Normalize(platform.Point2 - platform.Point1);
            var normal = new Vector2(-tangent.Y, tangent.X);
            var velocity = Collider._body.LinearVelocity;
            var offset1 = Transform.Position - platform.Point1;
            var offset2 = Transform.Position - platform.Point2;
            var movesToFrontSide = Vector2.Dot(normal, velocity) <= 0;
            var centerAbove = Vector2.Dot(offset1, normal) >= 0;

            if (!movesToFrontSide || !centerAbove)
                return false;

            bool atLeft = Vector2.Dot(tangent, offset1) < 0;
            bool atRight = Vector2.Dot(tangent, offset2) > 0;
            bool movesAtLeft = Vector2.Dot(tangent, velocity) < 0;
            bool movesAtRight = Vector2.Dot(tangent, velocity) > 0;
            bool inputLeft = Input.Move.X < 0;
            bool inputRight = Input.Move.X > 0;

            if (Board.OnGround && !atLeft && !atRight)
            {
                _platformChanged = true;
                Board.CurrentPlatform = platform;
                var alongsideScale = Sign(Vector2.Dot(velocity, tangent));
                var nextVelocity = tangent * velocity.Length() * alongsideScale;
                Collider._body.LinearVelocity = Vector2.Zero;
                Core.Logic.Gameplay.Physics.DoAfterStep(() =>
                {
                    var depth = Board.Specs.ColliderRadius - Vector2.Dot(offset1, normal);
                    //Transform.Position += normal * depth;
                    Collider._body.LinearVelocity = nextVelocity;
                });

                return true;
            }

            if (!Board.OnGround && !atLeft && !atRight)
            {
                _platformChanged = true;
                Board.CurrentPlatform = platform;
                var alongsideScale = Sign(Vector2.Dot(velocity, tangent));
                var nextVelocity = tangent * velocity.Length() * alongsideScale;
                Collider._body.LinearVelocity = Vector2.Zero;
                Core.Logic.Gameplay.Physics.DoAfterStep(() =>
                {
                    var depth = Board.Specs.ColliderRadius - Vector2.Dot(offset1, normal);
                    //Transform.Position += normal * depth;
                    Collider._body.LinearVelocity = nextVelocity;
                    Enter(new RunState(this));
                });

                return true;
            }

            if (!Board.OnGround && atLeft && !inputLeft)
            {
                _platformChanged = true;
                Board.CurrentPlatform = platform;
                var alongsideScale = Vector2.Dot(velocity, tangent) > 0 ? 1 : 0;
                var nextVelocity = tangent * velocity.Length() * alongsideScale;
                Core.Logic.Gameplay.Physics.DoAfterStep(() =>
                {
                    //Transform.Position = platform.Point1 + normal * Board.Specs.ColliderRadius;
                    Collider._body.LinearVelocity = nextVelocity;
                    Enter(new RunState(this));
                });
                return true;
            }
            
            if (!Board.OnGround && atRight && !inputRight)
            {
                _platformChanged = true;
                Board.CurrentPlatform = platform;
                var alongsideScale = Vector2.Dot(velocity, tangent) < 0 ? -1 : 0;
                var nextVelocity = tangent * velocity.Length() * alongsideScale;
                Core.Logic.Gameplay.Physics.DoAfterStep(() =>
                {
                    //Transform.Position = platform.Point2 + normal * Board.Specs.ColliderRadius;
                    Collider._body.LinearVelocity = nextVelocity;
                    Enter(new RunState(this));
                });
                return true;
            }

            return false;
        };

        Collider.OnSeparation += data =>
        {
            if (data.Other.GameObject.TryGetComponent<Platform>(out var platform) &&
                platform == Board.CurrentPlatform)
            {
                Board.CurrentPlatform = null;
            }
        };
    }

    public override void Update()
    {
        base.Update();
        _platformChanged = false;
    }
}

public record PlayerBoard
{
    public PlayerSpecs Specs = new();

    [MemberNotNullWhen(true, nameof(CurrentPlatform))]
    public bool OnGround => CurrentPlatform is not null;
    public Platform? CurrentPlatform;
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
        [Inspect(min: 0)] public float Speed = 15;
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
        [Inspect(min: 0)] public float FallHorizontalSpeed = 10;
        [Inspect(min: 0)] public float FallVerticalAcceleration = 15;
        [Inspect(min: 0)] public float FallVerticalSpeedLimit = 30;
    }
}