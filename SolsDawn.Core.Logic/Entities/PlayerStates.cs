using System.Linq;
using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Combat;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using Collider = SolsDawn.Core.Logic.Gameplay.Collider;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Entities;

[Flags]
public enum AllowedState
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

public class StateCategoryAttribute : CategoryAttribute
{
    public AllowedState State { get; }

    public StateCategoryAttribute(AllowedState state)
    {
        State = state;
    }

    public override string GetTagsDescription() => $"State: {State}";
}

public class StateModelAttribute : ModelAttribute
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

public abstract class PlayerState : Core.Logic.Gameplay.State<Player>;

public abstract class JumpState : PlayerState;

public abstract class BladeState : PlayerState;

public abstract class FireState : PlayerState;

public abstract class DownAttackState : PlayerState;

[StateModel(AllowedState.Idle)]
public class IdleState : PlayerState
{
    public override async Job Job(Player player)
    {
        player.Board.Velocity = Vector2.Zero;
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
    }
}

[StateModel(AllowedState.Run)]
public class RunState : PlayerState
{
    [MovementCategory]
    public CategorySlot<IMovementModel> Movement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();

        while (true)
        {
            var currentPlatform = board.CurrentPlatform;
            if (currentPlatform is null)
            {
                Log.Logic.Error("Platform can't be null while run");
                return;
            }

            var angle = currentPlatform.Transform.Rotation;
            var tangent = new Vector2(Cos(angle), Sin(angle));

            var inDir = Input.Move;
            var vx = Vector2.Dot(board.Velocity, tangent);

            vx = Movement.GetActiveInstance().Calculate(vx, inDir, DeltaTime, ref memory);

            board.Velocity = new Vector2(vx, 0).Rotated(angle);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Jump)]
[Default]
public class CelesteJumpState : JumpState
{
    [Inspect(min: 0, max: PI / 2)] public float HorizontalDashAngle = PI / 6;
    [Inspect(min: 0)] public float HorizontalDashDistance = 5;
    [Inspect(min: 0)] public float VerticalDashDistance = 5;
    [Inspect(min: 1)] public int DashFrames = 3;

    [Inspect(min: 0)] public float HorizontalDashFinalSpeed = 20;
    [Inspect(min: 0)] public float VerticalDashFinalSpeed = 20;
    [Inspect(min: 0)] public float VerticalFriction = 15;

    [MovementCategory]
    public CategorySlot<IMovementModel> HorizontalMovement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();

        {
            var initInDir = Input.Move;
            if (initInDir == 0)
            {
                var vy = VerticalDashDistance / (DashFrames * DeltaTime);
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = HorizontalDashDistance / (DashFrames * DeltaTime);
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }

            for (int i = 0; i < DashFrames; i++)
            {
                await NextFrame();
            }

            if (initInDir == 0)
            {
                var vy = VerticalDashFinalSpeed;
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = HorizontalDashFinalSpeed;
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while jump");
                return;
            }

            var inDir = Input.Move;
            var vx = HorizontalMovement.GetActiveInstance().Calculate(board.Velocity.X, inDir, DeltaTime, ref memory);
            var vy = board.Velocity.Y - VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Jump)]
public class PrimitiveJumpState : JumpState
{
    [Inspect(min: 0)] public float HorizontalStartSpeed = 30;
    [Inspect(min: 0)] public float VerticalStartSpeed = 20;
    [Inspect(min: 0)] public float VerticalFriction = 15;

    [MovementCategory]
    public CategorySlot<IMovementModel> HorizontalMovement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();

        {
            var inDir = Input.Move;
            board.Velocity = new Vector2(inDir * HorizontalStartSpeed, VerticalStartSpeed);
            await NextFrame();
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while jump");
                return;
            }

            var inDir = Input.Move;
            var vx = HorizontalMovement.GetActiveInstance().Calculate(board.Velocity.X, inDir, DeltaTime, ref memory);
            var vy = board.Velocity.Y - VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Fall)]
public class FallState : PlayerState
{
    [Inspect(min: 0)] public float VerticalAcceleration = 15;
    [Inspect(min: 0)] public float VerticalMaxSpeed = 30;

    [MovementCategory]
    public CategorySlot<IMovementModel> HorizontalMovement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Input.Move;
            var vx = HorizontalMovement.GetActiveInstance().Calculate(board.Velocity.X, inDir, DeltaTime, ref memory);
            var vy = Max(-VerticalMaxSpeed, board.Velocity.Y - VerticalAcceleration * DeltaTime);

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}
internal static class AttackMotion
{
    public static void Step(PlayerBoard board, AR movement, ref Vector2 axis, ref float speed, ref MovementMemory memory)
    {
        if (board.CurrentPlatform is { } platform)
            axis = CombatMath.TangentProjection(axis, platform.Transform.WorldRotation);

        speed = movement.Calculate(speed, Sign.Positive, DeltaTime, ref memory);
        board.Velocity = axis * speed;
    }
}

[StateModel(AllowedState.Blade)]
public class SlideBlade : BladeState
{
    [EnvelopeCategory(EnvelopeShape.AR)]
    public CategorySlot<AR> Movement = new();

    [Inspect(0.01f, 0.01f, 10f)] public float Length = 0.6f;
    [Inspect(0.01f, 0.01f, 10f)] public float HalfWidth = 0.5f;
    [Inspect(0.01f, 0f, 10f)] public float TipLength = 0.6f;
    [Inspect(0.01f, 0f, 5f)] public float HitboxStart = 0f;
    [Inspect(0.01f, 0f, 5f)] public float HitboxEnd = 0.3f;
    [Inspect(0.01f, 0f, 5f)] public float ControlBackTime = 0.3f;

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var axis = CombatMath.Direction(player.Transform.Position, board.AttackPointer);
        var speed = board.Velocity.Length();
        var memory = new MovementMemory();
        var time = 0f;
        Attack? attack = null;

        while (true)
        {
            AttackMotion.Step(board, Movement.GetActiveInstance(), ref axis, ref speed, ref memory);

            if (attack is null && time >= HitboxStart && time < HitboxEnd)
                attack = CreateHitbox(player);
            if (attack is { IsDestroyed: false })
            {
                if (time >= HitboxEnd)
                    attack.Destroy();
                else
                    attack.Transform.Rotation = axis.Angle();
            }

            if (time >= ControlBackTime)
                board.ControlBack = true;

            if (AR.IsDone(memory))
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
public class SimpleFire : FireState
{
    [EnvelopeCategory(EnvelopeShape.AR)]
    public CategorySlot<AR> Movement = new();

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
        var axis = CombatMath.Direction(center, board.AttackPointer);
        Effects.Start(() => Shot(center, axis));

        var moveAxis = -axis;
        var speed = board.Velocity.Length();
        var memory = new MovementMemory();
        var time = 0f;

        while (true)
        {
            AttackMotion.Step(board, Movement.GetActiveInstance(), ref moveAxis, ref speed, ref memory);

            if (time >= ControlBackTime)
                board.ControlBack = true;

            if (AR.IsDone(memory))
                return;

            await NextFrame();
            time += DeltaTime;
        }
    }

    private async Job Shot(Vector2 center, Vector2 axis)
    {
        var end = center + axis * Distance;

        var attackObject = CreateObject(center + axis * Distance / 2, axis.Angle());
        var attack = new Attack(attackObject, Shapes.Rectangle(Distance, Width), Side.Enemy, ParryKind.Fire);
        attack.Parried.OnFire(() =>
        {
            var t = CombatMath.FireParryDistance(
                center,
                axis,
                attack.ParriedBoxes.Select(b => b.Transform.WorldPosition),
                Distance);
            var parryPoint = center + axis * t;
            attack.ParryPoint = parryPoint;
            end = parryPoint;

            if (t > 0.001f)
            {
                attackObject.Transform.Position = center + axis * t / 2;
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
public class SimpleDownAttack : DownAttackState
{
    [EnvelopeCategory(EnvelopeShape.AS)]
    public CategorySlot<AS> Vertical = new();

    [AdsrCategory(AllowedAdsrState.Release)]
    public CategorySlot<IAdsrModel> Horizontal = new();

    [Inspect(0.01f, 0.01f, 10f)] public float Length = 0.6f;
    [Inspect(0.01f, 0.01f, 10f)] public float HalfWidth = 0.5f;
    [Inspect(0.01f, 0f, 10f)] public float TipLength = 0.6f;

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var verticalSpeed = Max(0, -board.Velocity.Y);
        var vx = board.Velocity.X;
        Sign hDir = vx;
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
            verticalSpeed = Vertical.GetActiveInstance().Calculate(verticalSpeed, Sign.Positive, DeltaTime, ref memory);
            vx = Horizontal.GetActiveInstance().Calculate(vx, hDir, hDir, DeltaTime, 0f);
            board.Velocity = new Vector2(vx, -verticalSpeed);

            if (!attack.IsDestroyed)
                attackObject.Transform.Rotation = board.Velocity == Vector2.Zero ? -PI / 2 : board.Velocity.Angle();

            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Bounce)]
public class BounceState : PlayerState
{
    [Inspect(min: 0)] public float VerticalStartSpeed = 15;
    [Inspect(min: 0)] public float VerticalFriction = 15;

    [MovementCategory]
    public CategorySlot<IMovementModel> HorizontalMovement = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        var memory = new MovementMemory();
        board.Velocity = new Vector2(0, VerticalStartSpeed);
        await NextFrame();

        while (true)
        {
            var inDir = Input.Move;
            var vx = HorizontalMovement.GetActiveInstance().Calculate(board.Velocity.X, inDir, DeltaTime, ref memory);
            var vy = board.Velocity.Y - VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}
