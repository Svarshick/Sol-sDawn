using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Collision;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Combat;
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
            _player.Hurtbox.Collider.Shape = Shapes.Circle(_colliderRadius);
        }

        if (_player.State is null)
            Log.Logic.Fatal("Player state can't be null");

        var state = _player.State;
        var center = _player.Transform.Position;
        var canAttack = state is IdleState or RunState or JumpState or FallState or BounceState;
        var canDownAttack = state is JumpState or FallState or BounceState or BladeState;

        if (canDownAttack && !board.OnGround && Input.Jump)
        {
            board.DownAttackParried = false;
            _player.Enter(specs.DownAttack.GetActiveInstance());
        }
        else if (canAttack && TryPointer(Input.Blade, center, out var bladePointer))
        {
            board.AttackPointer = bladePointer;
            _player.Enter(specs.Blade.GetActiveInstance());
        }
        else if (canAttack && TryPointer(Input.Fire, center, out var firePointer))
        {
            board.AttackPointer = firePointer;
            _player.Enter(specs.Fire.GetActiveInstance());
        }
        else if (state is BladeState or FireState or DownAttackState)
        {
            if (_player.StateJob is not { IsEnded: false })
            {
                if (state is DownAttackState && board.DownAttackParried)
                    _player.Enter(specs.Bounce.GetActiveInstance());
                else
                    EnterGroundedOrFall();
            }
        }
        else if (state is BounceState)
        {
            if (board.OnGround || board.Velocity.Y <= 0)
                EnterGroundedOrFall();
        }
        else if (_player.State is IdleState or RunState)
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

        board.MovementCastHit = result.Hit;
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

    private void EnterGroundedOrFall()
    {
        var board = _player.Board;
        var specs = board.Specs.GetActiveInstance();
        if (!board.OnGround)
            _player.Enter(specs.Fall.GetActiveInstance());
        else if (board.Velocity == Vector2.Zero && Input.Move == Vector2.Zero)
            _player.Enter(specs.Idle.GetActiveInstance());
        else
            _player.Enter(specs.Run.GetActiveInstance());
    }

    private static bool TryPointer((Vector2 ScreenPosition, bool IsPressed) input, Vector2 center, out Vector2 pointer)
    {
        pointer = Camera.ScreenToWorld(input.ScreenPosition);
        return input.IsPressed && CombatMath.IsValidPointer(center, pointer);
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