using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Collision;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay.Entities;

public class PlayerController
{
    private readonly Player _player;
    private float _colliderRadius;

    public PlayerController(Player player)
    {
        _player = player;
        _colliderRadius = _player.Board.Specs.GetActiveInstance().ColliderRadius;
        _player.Enter(_player.Board.Specs.GetActiveInstance().Idle.GetActiveInstance());
    }

    public void Update()
    {
        var board = _player.Board;
        var specs = board.Specs.GetActiveInstance();

        if (specs.ColliderRadius != _colliderRadius)
        {
            _colliderRadius = specs.ColliderRadius;
            _player.Collider.Shape = Shapes.Circle(_colliderRadius);
        }

        if (_player.State is null)
            Log.Logic.Fatal("Player state can't be null");

        if (_player.State is IdleState or RunState)
        {
            if (!board.OnGround)
            {
                _player.Enter(specs.Fall.GetActiveInstance());
            }
            else if (Input.Jump)
            {
                _player.Enter(specs.Jump.GetActiveInstance());
            }
            else if (_player.State is IdleState && Input.Move != Vector2.Zero)
            {
                _player.Enter(specs.Run.GetActiveInstance());
            }
            else if (_player.State is RunState && board.Velocity == Vector2.Zero && Input.Move == Vector2.Zero)
            {
                _player.Enter(specs.Idle.GetActiveInstance());
            }
        }
        else if (_player.State is FallState or JumpState)
        {
            if (board.OnGround)
            {
                if (board.Velocity == Vector2.Zero && Input.Move == Vector2.Zero)
                    _player.Enter(specs.Idle.GetActiveInstance());
                else
                    _player.Enter(specs.Run.GetActiveInstance());
            }
            else if (_player.State is JumpState && board.Velocity.Y <= 0)
            {
                _player.Enter(specs.Fall.GetActiveInstance());
            }
        }

        _player.UpdateState();

        var delta = board.Velocity * DeltaTime;
        var result = Physics.CircleCast(
            _player.Transform.Position,
            _player.Transform.Position + delta,
            specs.ColliderRadius,
            Layer.Wall,
            OneWayPlatformFilter);

        if (result.Hit)
        {
            _player.Transform.Position += delta * result.Fraction;
            board.CurrentPlatform = result.Collider.GameObject.GetComponent<Platform>();
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

        var specs = _player.Board.Specs.GetActiveInstance();
        var xfA = _player.Collider.Pose;
        var xfB = platform.Collider.Pose;
        return Collision.TestOverlap(
            Shapes.Circle(specs.ColliderRadius + Settings.LinearSlop),
            0,
            platform.Edge,
            0,
            ref xfA,
            ref xfB);
    }

    private bool OneWayPlatformFilter(Vector2 from, Vector2 to, float radius, float fraction, Collider collider)
    {
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
}