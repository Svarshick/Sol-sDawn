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

        if (SelectState(board, specs) is { } next)
        {
            board.ControlBack = false;
            board.Parried = false;
            _player.Enter(next);
        }

        _player.UpdateState();

        var delta = board.Velocity * DeltaTime;
	if (board.CurrentPlatform is { } ground)
	{
            var t = ground.Point2 - ground.Point1;
            var n = Vector2.Normalize(new Vector2(-t.Y, t.X));
	    delta -= n * Min(0, Vector2.Dot(delta, n));
	}
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

    private PlayerState? SelectState(PlayerBoard board, PlayerSpecs specs)
    {
        var state = _player.State;
        var center = _player.Transform.Position;
        var ground = board.OnGround;
        var air = !ground;
        var run = Input.Move != 0;
        var still = board.Velocity == Vector2.Zero;
        var moving = !still;
        var stateEnded = _player.StateJob is not { IsEnded: false };
        var controlBack = board.ControlBack;
        var jump = ground && Input.Jump;
        var downA = air && Input.Jump;
        var blade = TryPointer(Input.Blade, center, out var bladePointer);
        var fire = TryPointer(Input.Fire, center, out var firePointer);

        switch (state)
        {
            case IdleState:
                if (blade) return Blade();
                if (fire) return Fire();
                if (jump) return specs.Jump.GetActiveInstance();
                if (ground && run) return specs.Run.GetActiveInstance();
                if (air) return specs.Fall.GetActiveInstance();
                return null;

            case RunState:
                if (blade) return Blade();
                if (fire) return Fire();
                if (jump) return specs.Jump.GetActiveInstance();
                if (ground && !run && still) return specs.Idle.GetActiveInstance();
                if (air) return specs.Fall.GetActiveInstance();
                return null;

            case JumpState or FallState:
                if (blade) return Blade();
                if (fire) return Fire();
                if (downA) return specs.DownAttack.GetActiveInstance();
                if (ground && (run || moving)) return specs.Run.GetActiveInstance();
                if (ground && still) return specs.Idle.GetActiveInstance();
                if (state is JumpState && air && board.Velocity.Y <= 0) return specs.Fall.GetActiveInstance();
                return null;

            case BounceState:
                if (blade) return Blade();
                if (fire) return Fire();
                if (ground && (run || moving)) return specs.Run.GetActiveInstance();
                if (ground && still) return specs.Idle.GetActiveInstance();
                if (air && board.Velocity.Y <= 0) return specs.Fall.GetActiveInstance();
                return null;

            case BladeState or FireState:
                if (controlBack && state is BladeState && fire) return Fire();
                if (controlBack && state is FireState && blade) return Blade();
                if (controlBack && downA) return specs.DownAttack.GetActiveInstance();
                if (ground && ((controlBack && run) || (stateEnded && moving))) return specs.Run.GetActiveInstance();
                if (ground && stateEnded && still) return specs.Idle.GetActiveInstance();
                if (air && stateEnded && board.Velocity.Y <= 0) return specs.Fall.GetActiveInstance();
                return null;

            case DownAttackState:
                if (board.Parried) return specs.Bounce.GetActiveInstance();
                if (ground && (run || moving)) return specs.Run.GetActiveInstance();
                if (ground && still) return specs.Idle.GetActiveInstance();
                return null;

            default:
                return null;
        }

        PlayerState Blade()
        {
            board.AttackPointer = bladePointer;
            return specs.Blade.GetActiveInstance();
        }

        PlayerState Fire()
        {
            board.AttackPointer = firePointer;
            return specs.Fire.GetActiveInstance();
        }
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
