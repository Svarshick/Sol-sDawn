using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class BossStats
{
    public Color Color;
    public float Width;
    public float Height;

    public float HitDuration;
    public Color HitBlinkColor;

    public float BladeTelegraphDuration;
    public Color BladeTelegraphBlinkColor;
    public float BladeTelegraphStarDistance;
    public float BladeTelegraphStarDuration;
    public Color BladeTelegraphStarColor;
    public float BladeTelegraphStarOuterRadius;
    public float BladeTelegraphStarInnerRadius;
    [Euler] public float BladeTelegraphStarStartAngle;
    [Euler] public float BladeTelegraphStarDeltaAngle;
    public float BladeTelegraphStarThickness;

    public float BladeAttackDistance;
    [Euler] public float BladeAttackEdgeAngle;
    public float BladeAttackEdgeLength;
    public float BladeAttackEdgeWidth;
    public float BladeDashDistance;
    public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;
    public Color BladeTraceEndColor;

    public float BladeParriedDuration;
    public Color BladeParriedColor;
    public float BladeParriedPushDistance;
    public float BladeParriedPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireTelegraphDuration;
    public float FireTelegraphAimingDuration;
    public Color FireTelegraphBlinkColor;

    public float FireDistance;
    public float FireWidth;
    public float FireTraceDuration;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;

    public float FireParriedDuration;
    public Color FireParriedColor;
    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;

    public float TeleportTraceWidth;
    public float TeleportTraceDuration;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

/*public sealed class Boss : Component<Boss>, IUpdatable
{
    public readonly BossStats Stats;

    public State State { get; private set; }

    private readonly BossAnimations _animations;
    private readonly Collider _collider;
    private readonly HP _hp;

    public Boss(GameObject go) : base(go)
    {
        Stats = MainConfig.BossStats;

        _collider = go.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _hp = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
        var animator = go.GetComponent<Animator<BossAnimations>>() ?? throw new ComponentNotFoundException<Animator<BossAnimations>>();
        _animations = animator.Player;

        State = new PendingState(this);
    }

    public override void Dispose()
    {
    }

    public void Update()
    {
        var bounds = BoundingBox2D.CreateFromCenterAndExtents(GameObject.Transform.Position, new Vector2(Stats.Width/2, Stats.Height/2));
        _collider.Shape = new CollisionShape2D(bounds);
        State.Update();
    }

    public void LateUpdate()
    {
        State.LateUpdate();
    }

    public void Enter(State state)
    {
        State.Exit(state);
        state.Enter(State);
        State = state;
    }

    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Boss] Damaged : {value}");
        _animations.TryPlay(BossAnimations.Hit);
    }

    public class PendingState(
        Boss boss)
        : State
    {
    }

    public class IdleState(
        Boss boss,
        double duration)
        : State
    {
        public double TimeLeft = duration;

        public override void Enter(State from)
        {
            boss._animations.TryPlay(BossAnimations.Idle);
        }

        public override void Update()
        {
            TimeLeft -= Time.ElapsedGameTime.TotalSeconds;
            if (TimeLeft < 0)
            {
                var pending = new PendingState(boss);
                IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
            }
        }
    }

    public class TeleportState(
        Boss boss,
        Vector2 position)
        : State
    {
        public override void Enter(State from)
        {
            Game.AnimationsPool.Add(new LineTrace(
                new Transform { Position = boss.GameObject.Transform.Position },
                position,
                boss.Stats.TeleportTraceWidth,
                boss.Stats.TeleportTraceDuration,
                boss.Stats.TeleportTraceStartColor,
                boss.Stats.TeleportTraceEndColor));

            boss.GameObject.Transform.Position = position;

            var pending = new PendingState(boss);
            IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
        }
    }

    public class BladeTelegraphState(
        Boss boss,
        FightBlackboard blackboard,
        Vector2 lookPosition)
        : State
    {
        public double TimeLeft = boss.Stats.BladeTelegraphDuration;
        public GameObject ParryGO;
        public Vector2 LookPosition = lookPosition;

        public override void Enter(State from)
        {
            boss._animations.LookPosition = LookPosition;
            boss._animations.TryPlay(BossAnimations.BladeTelegraph);
            var bladeDirection = LookPosition - boss.GameObject.Transform.Position;
            bladeDirection.Normalize();
            var bladeVertices = Helper.ArchVertices(
                boss.GameObject.Transform.Position,
                bladeDirection,
                boss.Stats.BladeDashDistance + boss.Stats.BladeAttackDistance,
                boss.Stats.Width,
                boss.Stats.BladeAttackEdgeAngle,
                boss.Stats.BladeAttackEdgeLength);
            ParryGO = new();
            var parryPolygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
            new Collider(ParryGO, 10, Collision.LayerName.Parry, new CollisionShape2D(parryPolygon));
            new Parry(ParryGO, boss.GameObject, ParryType.Blade);
        }

        public override void Update()
        {
            TimeLeft -= Time.ElapsedGameTime.TotalSeconds;
            if (TimeLeft < 0)
            {
                var bladeDirection = LookPosition - boss.GameObject.Transform.Position;
                bladeDirection.Normalize();
                var bladeVertices = Helper.ArchVertices(
                    boss.GameObject.Transform.Position,
                    bladeDirection,
                    boss.Stats.BladeDashDistance + boss.Stats.BladeAttackDistance,
                    boss.Stats.Width,
                    boss.Stats.BladeAttackEdgeAngle,
                    boss.Stats.BladeAttackEdgeLength);

                var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
                var shape = new CollisionShape2D(polygon);
                var targets = new List<GameObject>();
                Collision.Overlap(shape, Collision.LayerName.Player, targets);

                var execute = new BladeExecutionState(boss, blackboard, LookPosition, targets);
                IntentionsPool.AddIntention(Intend(boss.GameObject, execute));
            }
        }

        public override void Exit(State to)
        {
            ParryGO.Dispose();
            ParryGO = null;
        }
    }

    public class BladeExecutionState(
        Boss boss,
        FightBlackboard blackboard,
        Vector2 lookPosition,
        IReadOnlyList<GameObject> targets)
        : State
    {
        public Vector2 LookPosition = lookPosition;
        public IReadOnlyList<GameObject> Targets = targets;

        public override void Enter(State from)
        {
            var direction = LookPosition - boss.GameObject.Transform.Position;
            direction.Normalize();

            Helper.DrawDashAttack(
                boss.GameObject.Transform.Position,
                direction,
                boss.Stats.BladeDashDistance,
                boss.Stats.BladeDashWidth,
                boss.Stats.BladeAttackDistance,
                boss.Stats.BladeAttackEdgeAngle,
                boss.Stats.BladeAttackEdgeLength,
                boss.Stats.BladeAttackEdgeWidth,
                boss.Stats.BladeTraceDuration,
                boss.Stats.BladeTraceStartColor,
                boss.Stats.BladeTraceEndColor);

            boss.GameObject.Transform.Position += direction * boss.Stats.BladeDashDistance;

            if (Targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(boss.GameObject, Targets, 1));
            }

            var pending = new PendingState(boss);
            IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
        }
    }

    public class BladeParriedState(
        Boss boss,
        FightBlackboard blackboard,
        Vector2 parryPosition)
        : State
    {
        public double TimeLeft = boss.Stats.BladeParriedDuration;
        public Vector2 PushPosition;

        public override void Enter(State from)
        {
            var crash = parryPosition - boss.GameObject.Transform.Position;
            var crashDistance = crash.Length();
            Vector2 direction;
            if (crashDistance < float.Epsilon)
            {
                // Parry point is on top of the actor – use a fallback direction
                // (any non-zero vector is fine; the dash distance will be zero anyway).
                direction = Vector2.UnitX;
                crashDistance = 0f;
            }
            else
            {
                direction = crash / crashDistance;
            }

            var dashDistance = crashDistance > boss.Stats.BladeDashDistance
                ? boss.Stats.BladeDashDistance
                : 0;

            var bladeDistance = crashDistance > boss.Stats.BladeDashDistance
                ? crashDistance - boss.Stats.BladeDashDistance
                : crashDistance;

            Helper.DrawDashAttack(
                boss.GameObject.Transform.Position,
                direction,
                dashDistance,
                boss.Stats.BladeDashWidth,
                bladeDistance,
                boss.Stats.BladeAttackEdgeAngle,
                boss.Stats.BladeAttackEdgeLength,
                boss.Stats.BladeAttackEdgeWidth,
                boss.Stats.BladeParryTraceDuration,
                boss.Stats.BladeParryTraceStartColor,
                boss.Stats.BladeParryTraceEndColor);

            if (dashDistance > 0)
            {
                boss.GameObject.Transform.Position += direction * dashDistance;
            }

            PushPosition = boss.GameObject.Transform.Position - direction * boss.Stats.BladeParriedPushDistance;
            boss._animations.TryPlay(BossAnimations.BladeParried);
        }

        public override void Update()
        {
            if (boss.GameObject.Transform.Position != PushPosition)
            {
                var remain = PushPosition - boss.GameObject.Transform.Position;
                var direction = Vector2.Normalize(remain);
                var delta = direction * (float)(Time.ElapsedGameTime.TotalSeconds * boss.Stats.BladeParriedPushVelocity);
                if (delta.LengthSquared() >= remain.LengthSquared())
                {
                    boss.GameObject.Transform.Position = PushPosition;
                }
                else
                {
                    boss.GameObject.Transform.Position += delta;
                }
                return;
            }

            TimeLeft -= Time.ElapsedGameTime.TotalSeconds;
            if (TimeLeft < 0)
            {
                var pending = new PendingState(boss);
                IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
            }
        }
    }

    public class FireTelegraphState(
        Boss boss,
        FightBlackboard blackboard,
        Func<Vector2> lookPosition)
        : State
    {
        public double TimeLeft = boss.Stats.FireTelegraphDuration;
        public Vector2 FirePosition;

        public override void Enter(State from)
        {
            new Parry(boss.GameObject, boss.GameObject, ParryType.Fire);
            FirePosition = lookPosition.Invoke();
            boss._animations.LookPosition = FirePosition;
            boss._animations.TryPlay(BossAnimations.FireTelegraph);
        }

        public override void Update()
        {
            TimeLeft -= Time.ElapsedGameTime.TotalSeconds;

            if (TimeLeft < 0)
            {
                var direction = FirePosition - boss.GameObject.Transform.Position;
                direction.Normalize();
                var fireEnd = boss.GameObject.Transform.Position + direction * boss.Stats.FireDistance;

                var bounds = new OrientedBoundingBox2D(
                    (boss.GameObject.Transform.Position + fireEnd) / 2,
                    direction,
                    direction.PerpendicularClockwise(),
                    new Vector2(boss.Stats.FireDistance / 2, boss.Stats.FireWidth / 2));
                var shape = new CollisionShape2D(bounds);
                var targets = new List<GameObject>();
                Collision.Overlap(shape, Collision.LayerName.Player, targets);

                var execute = new FireExecutionState(boss, blackboard, FirePosition, targets);
                IntentionsPool.AddIntention(Intend(boss.GameObject, execute));
            }
            else if (TimeLeft > boss.Stats.FireTelegraphDuration - boss.Stats.FireTelegraphAimingDuration)
            {
                FirePosition = lookPosition.Invoke();
                boss._animations.LookPosition = FirePosition;
            }
        }

        public override void Exit(State to)
        {
            boss.GameObject.RemoveComponent<Parry>();
        }
    }

    public class FireExecutionState(
        Boss boss,
        FightBlackboard blackboard,
        Vector2 lookPosition,
        IReadOnlyList<GameObject> targets)
        : State
    {
        public IReadOnlyList<GameObject> Targets = targets;

        public override void Enter(State from)
        {
            var direction = lookPosition - boss.GameObject.Transform.Position;
            direction.Normalize();

            Game.AnimationsPool.Add(new LineTrace(
                new Transform{ Position = boss.GameObject.Transform.Position },
                boss.GameObject.Transform.Position + direction * boss.Stats.FireDistance,
                boss.Stats.FireWidth,
                boss.Stats.FireTraceDuration,
                boss.Stats.FireTraceStartColor,
                boss.Stats.FireTraceEndColor));

            if (Targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(boss.GameObject, Targets, 1));
            }

            var pending = new PendingState(boss);
            IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
        }
    }

    public class FireParriedState(
        Boss boss,
        FightBlackboard blackboard,
        Vector2 parryPosition)
        : State
    {
        public double TimeLeft = boss.Stats.FireParriedDuration;
        public override void Enter(State from)
        {
            Game.AnimationsPool.Add(new LineTrace(
                new Transform { Position = boss.GameObject.Transform.Position },
                parryPosition,
                boss.Stats.FireWidth,
                boss.Stats.FireParryTraceDuration,
                boss.Stats.FireParryTraceStartColor,
                boss.Stats.FireParryTraceEndColor));
        }

        public override void Update()
        {
            TimeLeft -= Time.ElapsedGameTime.TotalSeconds;
            if (TimeLeft < 0)
            {
                var pending = new PendingState(boss);
                IntentionsPool.AddIntention(Intend(boss.GameObject, pending));
            }
        }
    }
}*/