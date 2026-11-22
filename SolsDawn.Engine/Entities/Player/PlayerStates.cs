using System.Linq;
using System;
using Microsoft.Xna.Framework;
using Collider = SolsDawn.Engine.Collider;
using static SolsDawn.Engine.GameplayAPI;

namespace SolsDawn.Engine;

[Flags]
internal enum AllowedState
{
    Idle       = 1 << 0,
    Run        = 1 << 1,
    Jump       = 1 << 2,
    Fall       = 1 << 3,
    Blade      = 1 << 4,
    Fire       = 1 << 5,
    DownAttack = 1 << 6,
    Bounce     = 1 << 7,
    All        = Idle | Run | Jump | Fall | Blade | Fire | DownAttack | Bounce
}

internal class StateCategoryAttribute : CategoryAttribute
{
    public AllowedState State { get; }

    public StateCategoryAttribute(AllowedState state)
    {
        State = state;
    }

    public override string GetTagsDescription() => $"State: {State}";
}

internal class StateModelAttribute : ModelAttribute
{
    public AllowedState States { get; }

    public StateModelAttribute(AllowedState states)
    {
        States = states;
    }

    public override bool Satisfies(CategoryAttribute baseConstraint)
    {
        if (baseConstraint is not StateCategoryAttribute constraint)
            return false;

        return (States & constraint.State) == constraint.State;
    }

    public override int Breadth => System.Numerics.BitOperations.PopCount((uint)States);

    public override string GetTagsDescription() => $"States: {States}";
}

public abstract class PlayerState : State<Player>;

internal struct PlatformWatch
{
    private Platform? _last;

    public bool Changed(Platform? current)
    {
        var changed = current is not null && current != _last;
        _last = current;
        return changed;
    }

    public static Vector2 Tangent(Platform platform)
    {
        var angle = platform.Transform.WorldRotation;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}

internal struct AttackMotion
{
    public Vector2 Axis;
    public float Speed;

    public bool Step(PlayerBoard board, IAdsrModel movement, float peakSpeed)
    {
        if (Speed < peakSpeed)
            Speed = movement.Calculate(DeltaTime, Speed, 0f, peakSpeed);

        board.Velocity = Axis * Speed;
        return Speed >= peakSpeed;
    }

    public void SetAim(PlayerBoard board, IAimAxisModel model, Vector2 aim)
    {
        var tangent = board.CurrentPlatform is { } platform ? PlatformWatch.Tangent(platform) : Vector2.Zero;
        (Axis, Speed) = model.Calculate(board.Velocity, aim, tangent);
    }
}

[StateModel(AllowedState.Idle)]
internal class IdleState : PlayerState
{
    public override async Job Job(Player player)
    {
        player.Board.Velocity = Vector2.Zero;
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
    }
}

[StateModel(AllowedState.Run)]
internal class RunState : PlayerState
{
    [ProjectionCategory]
    public CategorySlot<IProjectionModel> Projection = new();

    [MovementCategory]
    public CategorySlot<IMovementModel> Movement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();
        var platforms = new PlatformWatch();
        var v = 0f;

        while (true)
        {
            var platform = board.CurrentPlatform;
            if (platform is null)
            {
                Log.Logic.Error("Platform can't be null while run");
                return;
            }

            var tangent = PlatformWatch.Tangent(platform);
            if (platforms.Changed(platform))
                v = Projection.GetActiveInstance().Calculate(board.Velocity, tangent);

            v = Movement.GetActiveInstance().Calculate(v, GameplayAPI.Input.Move, DeltaTime, ref memory);
            board.Velocity = v * tangent;
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Jump)]
internal class JumpState : PlayerState
{
    [Inspect(0.01f, 0f)] public float Duration = 0.3f;

    [MovementCategory]
    public CategorySlot<IMovementModel> Horizontal = new();

    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Vertical = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var horizontalMemory = new MovementMemory();
        var verticalMemory = new MovementMemory();
        var time = 0f;
        board.Velocity = new Vector2(board.Velocity.X, 0f);

        while (true)
        {
            var vx = Horizontal.GetActiveInstance().Calculate(board.Velocity.X, GameplayAPI.Input.Move, DeltaTime, ref horizontalMemory);
            var vy = Vertical.GetActiveInstance().Calculate(board.Velocity.Y, Sign.Positive, DeltaTime, ref verticalMemory);
            board.Velocity = new Vector2(vx, vy);

            if (time >= Duration)
                return;

            await NextFrame();
            time += DeltaTime;
        }
    }
}

[StateModel(AllowedState.Fall)]
internal class FallState : PlayerState
{
    [MovementCategory]
    public CategorySlot<IMovementModel> Horizontal = new();

    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Vertical = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var horizontalMemory = new MovementMemory();
        var verticalMemory = new MovementMemory();

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var vx = Horizontal.GetActiveInstance().Calculate(board.Velocity.X, GameplayAPI.Input.Move, DeltaTime, ref horizontalMemory);
            var vd = Vertical.GetActiveInstance().Calculate(-board.Velocity.Y, Sign.Positive, DeltaTime, ref verticalMemory);
            board.Velocity = new Vector2(vx, -vd);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Blade)]
internal class BladeState : PlayerState
{
    [Inspect(0.5f, 0f, 200f)] public float PeakSpeed = 15f;

    [AimAxisCategory(AllowedAimAxis.Blade)]
    public CategorySlot<IAimAxisModel> Axis = new();

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Movement = new();

    [Inspect(0.01f, 0.01f, 10f)] public float Length = 0.6f;
    [Inspect(0.01f, 0.01f, 10f)] public float HalfWidth = 0.5f;
    [Inspect(0.01f, 0f, 10f)] public float TipLength = 0.6f;
    [Inspect(0.01f, 0f, 5f)] public float HitboxStart = 0f;
    [Inspect(0.01f, 0f, 5f)] public float HitboxEnd = 0.3f;
    [Inspect(0.01f, 0f, 5f)] public float ControlBackTime = 0.3f;

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var aim = CombatMath.Direction(player.Transform.Position, board.AttackPointer);
        var motion = new AttackMotion();
        var platforms = new PlatformWatch();
        motion.SetAim(board, Axis.GetActiveInstance(), aim);
        platforms.Changed(board.CurrentPlatform);
        var time = 0f;
        Attack? attack = null;

        while (true)
        {
            if (platforms.Changed(board.CurrentPlatform))
                motion.SetAim(board, Axis.GetActiveInstance(), aim);

            var done = motion.Step(board, Movement.GetActiveInstance(), PeakSpeed);

            if (attack is null && time >= HitboxStart && time < HitboxEnd)
                attack = CreateHitbox(player);
            if (attack is { IsDestroyed: false })
            {
                if (time >= HitboxEnd)
                    attack.Destroy();
                else
                    attack.Transform.Rotation = motion.Axis.Angle();
            }

            if (time >= ControlBackTime)
                board.ControlBack = true;

            if (done)
                return;

            await NextFrame();
            time += DeltaTime;
        }
    }

    private Attack CreateHitbox(Player player)
    {
        var attackObject = CreateObject();
        attackObject.Transform.Parent = player.Transform;
        var attack = new Attack(attackObject, CombatMath.Pentagon(Length, HalfWidth, TipLength), Side.Enemy, ParryKind.Blade);
        attack.Parried.OnFire(() => attack.Destroy());
        return attack;
    }
}

[StateModel(AllowedState.Fire)]
internal class FireState : PlayerState
{
    [Inspect(0.5f, 0f, 200f)] public float PeakSpeed = 15f;

    [AimAxisCategory(AllowedAimAxis.Fire)]
    public CategorySlot<IAimAxisModel> Axis = new();

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Movement = new();

    [Inspect(0.01f, 0f, 5f)] public float ControlBackTime = 0.2f;
    [Inspect(0.5f, 0f, 200f)] public float Distance = 20f;
    [Inspect(0.01f, 0.01f, 10f)] public float Width = 0.3f;
    [Inspect(0.01f, 0f, 5f)] public float TraceDuration = 0.4f;
    [Inspect] public Color TraceColor = Color.OrangeRed;
    [Inspect(0.05f, 0.01f, 20f)] public float ExplosionRadius = 1.5f;
    [Inspect(0.01f, 0f, 5f)] public float ExplosionDuration = 0.5f;
    [Inspect] public Color ExplosionColor = Color.Orange;

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var center = player.Transform.Position;
        var aim = CombatMath.Direction(center, board.AttackPointer);
        Effects.Start(() => Shot(center, aim));

        var motion = new AttackMotion();
        var platforms = new PlatformWatch();
        motion.SetAim(board, Axis.GetActiveInstance(), aim);
        platforms.Changed(board.CurrentPlatform);
        var time = 0f;

        while (true)
        {
            if (platforms.Changed(board.CurrentPlatform))
                motion.SetAim(board, Axis.GetActiveInstance(), aim);

            var done = motion.Step(board, Movement.GetActiveInstance(), PeakSpeed);

            if (time >= ControlBackTime)
                board.ControlBack = true;

            if (done)
                return;

            await NextFrame();
            time += DeltaTime;
        }
    }

    private async Job Shot(Vector2 center, Vector2 aim)
    {
        var end = center + aim * Distance;

        var attackObject = CreateObject(center + aim * Distance / 2, aim.Angle());
        var attack = new Attack(attackObject, Shapes.Rectangle(Distance, Width), Side.Enemy, ParryKind.Fire);
        attack.Parried.OnFire(() =>
        {
            var t = CombatMath.FireParryDistance(
                center,
                aim,
                attack.ParriedBoxes.Select(b => b.Transform.WorldPosition),
                Distance);
            var parryPoint = center + aim * t;
            attack.ParryPoint = parryPoint;
            end = parryPoint;

            if (t > 0.001f)
            {
                attackObject.Transform.Position = center + aim * t / 2;
                attack.Collider.Shape = Shapes.Rectangle(t, Width);
            }
            else
            {
                attack.Destroy();
            }

            var radius = ExplosionRadius;
            var duration = ExplosionDuration;
            var color = ExplosionColor;
            Effects.Start(() => Explosion(parryPoint, radius, duration, color));
        });

        await attack.Resolved;
        Game.AnimationsPool.Add(new LineTraceAnimation(center, end, Width, TraceDuration, TraceColor));
    }

    private static async Job Explosion(Vector2 position, float radius, float duration, Color color)
    {
        var attack = new Attack(CreateObject(position), Shapes.Circle(radius), Side.Player | Side.Enemy, ParryKind.None);
        Game.AnimationsPool.Add(new CircleTraceAnimation(position, radius, duration, color));
        await attack.Resolved;
    }
}

[StateModel(AllowedState.DownAttack)]
internal class DownAttackState : PlayerState
{
    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Vertical = new();

    [AdsrCategory(AllowedAdsrState.Release)]
    public CategorySlot<IAdsrModel> Horizontal = new();

    [Inspect(0.01f, 0.01f, 10f)] public float Length = 0.6f;
    [Inspect(0.01f, 0.01f, 10f)] public float HalfWidth = 0.5f;
    [Inspect(0.01f, 0f, 10f)] public float TipLength = 0.6f;

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var h = Abs(board.Velocity.X);
        var h0 = h;
        Sign hDir = board.Velocity.X;
        var memory = new MovementMemory();

        var attackObject = CreateObject();
        attackObject.Transform.Parent = player.Transform;
        var attack = new Attack(attackObject, CombatMath.Pentagon(Length, HalfWidth, TipLength), Side.Enemy, ParryKind.Blade);
        attack.Parried.OnFire(() =>
        {
            attack.Destroy();
            board.Parried = true;
        });

        while (true)
        {
            var vd = Vertical.GetActiveInstance().Calculate(-board.Velocity.Y, Sign.Positive, DeltaTime, ref memory);
            if (h > 0f)
                h = Horizontal.GetActiveInstance().Calculate(DeltaTime, h, h0, 0f);
            board.Velocity = new Vector2(hDir * h, -vd);

            if (!attack.IsDestroyed)
                attackObject.Transform.Rotation = board.Velocity == Vector2.Zero ? -PI / 2 : board.Velocity.Angle();

            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Bounce)]
internal class BounceState : PlayerState
{
    [Inspect(0.5f, 0f, 200f)] public float PeakSpeed = 15f;

    [AxisCategory]
    public CategorySlot<IAxisModel> Axis = new();

    [AdsrCategory(AllowedAdsrState.Attack)]
    public CategorySlot<IAdsrModel> Movement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var motion = new AttackMotion();
        (motion.Axis, motion.Speed) = Axis.GetActiveInstance().Calculate(board.Velocity);

        while (true)
        {
            if (motion.Step(board, Movement.GetActiveInstance(), PeakSpeed))
                return;

            await NextFrame();
        }
    }
}
