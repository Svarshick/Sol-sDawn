using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class PlayerController
{
    private readonly Player _player;
    
    public PlayerController(Player player)
    {
        _player = player;
        _player.Enter(new IdleState(_player));
    }

    public void Update()
    {
        var board = _player.Board;

        if (_player.State is null)
            Log.Logic.Fatal("Player state can't be null");

        var previousState = _player.State;
        if (_player.State is IdleState or RunState)
        {
            if (!board.OnGround)
            {
                _player.Enter(new FallState(_player));
            }
            else if (Input.Jump)
            {
                _player.Enter(CreateJumpState());
            }
            else if (_player.State is IdleState && Input.Move != Vector2.Zero)
            {
                _player.Enter(new RunState(_player));
            }
            else if (_player.State is RunState && board.Velocity == Vector2.Zero && Input.Move == Vector2.Zero)
            {
                _player.Enter(new IdleState(_player));
            }
        }
        else if (_player.State is FallState or JumpState)
        {
            if (board.OnGround)
            {
                if (board.Velocity == Vector2.Zero && Input.Move == Vector2.Zero)
                    _player.Enter(new IdleState(_player));
                else
                    _player.Enter(new RunState(_player));
            }
            else if (_player.State is JumpState && board.Velocity.Y <= 0)
            {
                _player.Enter(new FallState(_player));
            }
        }

        if (_player.State == previousState)
            _player.StateJob?.Update();

        var delta = board.Velocity * DeltaTime;
        var result = Core.Logic.Gameplay.Physics.CircleCast(
            _player.Transform.Position,
            _player.Transform.Position + delta,
            board.Specs.ColliderRadius,
            Layer.Player,
            Layer.Wall,
            filter: OneWayPlatformFilter);

        if (result.Hit)
        {
            _player.Transform.Position += delta * result.Fraction;
            board.CurrentPlatform = (result.HitFixture.Tag as Core.Logic.Gameplay.Collider)!.GameObject.GetComponent<Platform>();
        }
        else
        {
            _player.Transform.Position += delta;

            if (_player.Board.CurrentPlatform != null &&
                !TestOnGround())
            {
                _player.Board.CurrentPlatform = null;
            }
        }
    }
    
    private bool TestOnGround()
    {
        var platform = _player.Board.CurrentPlatform;
        if (platform is null)
            return false;

        var xfA = new Transform(_player.Transform.WorldPosition, _player.Transform.WorldRotation);
        var xfB = new Transform(platform.Transform.WorldPosition, platform.Transform.WorldRotation);
        return Collision.TestOverlap(
            Shapes.Circle(_player.Board.Specs.ColliderRadius + Settings.LinearSlop),
            0,
            platform._edge,
            0,
            ref xfA,
            ref xfB);
    }

    private bool OneWayPlatformFilter(Vector2 from, Vector2 to, float radius, float fraction, Fixture fixture)
    {
        if (fixture.Tag is not Core.Logic.Gameplay.Collider collider)
            return false;

        if (!collider.GameObject.TryGetComponent<Platform>(out var platform))
            return false;

        if (platform == _player.Board.CurrentPlatform)
            return false;

        var at = from + (to - from) * fraction;
        var toCenter = at - platform.Point1;
        var unnormalizedTangent = platform.Point2 - platform.Point1;
        var unnormalizedNormal = new Vector2(-unnormalizedTangent.Y, unnormalizedTangent.X);

        bool facesPlatform = Vector2.Dot(to - from, unnormalizedNormal) <= 0;
        bool abovePlatform = Vector2.Dot(toCenter, unnormalizedNormal) > 0;
        var centerProjectionFraction = Vector2.Dot(toCenter, unnormalizedTangent) / unnormalizedTangent.LengthSquared();
        bool bottomTouches = centerProjectionFraction is >= 0 and <= 1;
        return facesPlatform && abovePlatform && bottomTouches;
    }

    private JumpState CreateJumpState() => 
        Debug.CurrentJumpModel.Name switch
        {
            nameof(PrimitiveJumpState) => new PrimitiveJumpState(_player),
            nameof(CelesteJumpState) => new CelesteJumpState(_player),
            _ => throw Log.Logic.FatalException()
        };
}