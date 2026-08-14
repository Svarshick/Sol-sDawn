using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay.Animations;
using static SolsDawn.Core.Logic.Gameplay.Behaviour.BehaviourAPI;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class PlayerStats
{
    public Color Color;
    public float Width;
    public float Height;
    public float Velocity;
    public float CursorRadius;
    public Color CursorColor;

    public float TeleportRechargeDuration;
    public float BladeRechargeDuration;
    public float FireRechargeDuration;

    public float HitInvulnerabilityDuration;
    public Color HitBlinkColor;

    public float BladeAttackDistance;
    public float BladeAttackWidth;
    public float BladeDashDistance;
    public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;

    public float BladeParryPushDistance;
    public float BladeParryPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireDistance;
    public float FireWidth;
    public float FireTraceDuration;
    public float FireTraceWidth;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;

    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;

    public float TeleportMinDistance;
    public float TeleportMaxDistance;
    public float TeleportHoldDuration;
    public float TeleportWidth;
    public Color TeleportStartColor;
    public Color TeleportEndColor;
    public float TeleportTraceWidth;
    public float TeleportTraceDuration;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Player : Component
{
    public readonly PlayerStats Stats;
    
    public State State { get; private set; }
    public Routine StateRoutine { get; private set; }

    public double LastTeleportUsage;
    public double LastBladeUsage;
    public double LastFireUsage;

    public bool TeleportCharged => Time.TotalGameTime.TotalSeconds - LastTeleportUsage > Stats.TeleportRechargeDuration;
    public bool BladeCharged => Time.TotalGameTime.TotalSeconds - LastBladeUsage > Stats.BladeRechargeDuration;
    public bool FireCharged => Time.TotalGameTime.TotalSeconds - LastFireUsage > Stats.FireRechargeDuration;

    private readonly Collider _collider;
    private readonly PlayerAnimations _animations;
    
    public Player(GameObject go) : base(go, true)
    {
        Stats = MainConfig.PlayerStats;

        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        var animator = go.GetComponent<Animator<PlayerAnimations>>() ?? throw new ComponentNotFoundException<Animator<PlayerAnimations>>();
        _animations = animator.Player;

        var state = new IdleState(this);
        state.Enter(null);
        State = state;
        StateRoutine = new Routine(state.Update);
    }

    public void Enter(State state)
    {
        State.Exit(state);
        if (StateRoutine is null)
        {
            Console.WriteLine($"[Warning] previous state {State} routine is null");
        }
        else
        {
            StateRoutine.Kill();
        }

        state.Enter(State);
        State = state;
        StateRoutine = new Routine(state.Update);
    }

    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Player] Damaged : {value}");
        _animations.TryPlay(PlayerAnimations.Hit);
    }


    public class IdleState(Player player) : State
    {
        public override void Enter(State from)
        {
            player._animations.TryPlay(PlayerAnimations.Idle);
        }
    }

    public class MoveState(
        Player player,
        Input input) 
        : State
    {
        public Vector2 Direction;

        public override void Enter(State from)
        {
            Direction = input.Move;
        }

        public override async Task Update()
        {
            while (true)
            {
                Direction = input.Move;
                player.GameObject.Transform.Position +=
                    Direction * player.Stats.Velocity * (float)Time.ElapsedGameTime.TotalSeconds;
                await NextFrame();
            }
        }
    }

    public class TeleportState(
        Player player,
        Input input)
        : State
    {
        public Vector2 ScreenPosition = input.Teleport.ScreenPosition;
        public double ElapsedTime;

        private LineIdleAnimation _teleportLine;

        public override void Enter(State from)
        {
            ScreenPosition = input.Teleport.ScreenPosition;
            var mousePosition = SolsDawn.Camera.ScreenToWorld(ScreenPosition);
            var endPosition = TeleportPosition(mousePosition, 0);
            _teleportLine = new(player.GameObject.Transform.Position, endPosition, player.Stats.TeleportWidth, player.Stats.TeleportStartColor);
            SolsDawn.AnimationsPool.Add(_teleportLine);
        }
        
        public override async Task Update()
        {
            while (true)
            {
                ScreenPosition = input.Teleport.ScreenPosition;
                ElapsedTime += Time.ElapsedGameTime.TotalSeconds;
                var mousePosition = SolsDawn.Camera.ScreenToWorld(ScreenPosition);
                var lerp = TeleportLerp(ElapsedTime);
                _teleportLine.Transform.Position = player.GameObject.Transform.Position;
                _teleportLine.End = TeleportPosition(mousePosition, lerp);
                _teleportLine.Color = Color.Lerp(player.Stats.TeleportStartColor, player.Stats.TeleportEndColor, lerp);
                await NextFrame();
            }
        }

        public override void Exit(State to)
        {
            ScreenPosition = input.Teleport.ScreenPosition;
            _teleportLine.Kill();
            var mousePosition = SolsDawn.Camera.ScreenToWorld(ScreenPosition);
            var lerp = TeleportLerp(ElapsedTime);
            var endPosition = TeleportPosition(mousePosition, lerp);

            SolsDawn.AnimationsPool.Add(new LineTraceAnimation(
                player.GameObject.Transform.Position,
                endPosition,
                player.Stats.TeleportTraceWidth,
                player.Stats.TeleportTraceDuration,
                player.Stats.TeleportTraceStartColor));

            player.GameObject.Transform.Position = endPosition;
            player.LastTeleportUsage = Time.TotalGameTime.TotalSeconds;
        }

        private float TeleportLerp(double elapsedTime)
        {
            return MathHelper.Clamp((float)(elapsedTime / player.Stats.TeleportHoldDuration), 0f, 1f);
        }

        private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
        {
            var teleportDirection = pointPosition - player.GameObject.Transform.Position;
            teleportDirection.Normalize();
            var delta = lerp * (player.Stats.TeleportMaxDistance - player.Stats.TeleportMinDistance);
            return player.GameObject.Transform.Position + teleportDirection * (player.Stats.TeleportMinDistance + delta);
        }
    }

    public class BladeState(
        Player player,
        Vector2 lookPosition) 
        : State
    {
        public override async Task Update()
        {
            var direction= lookPosition - player.GameObject.Transform.Position;
            direction.Normalize();
            var bladeVertices = Helper.ArrowPentagonVertices(
                Vector2.Zero,
                new Vector2(1, 0),
                player.Stats.BladeDashDistance,
                player.Stats.BladeDashWidth,
                player.Stats.BladeAttackDistance,
                player.Stats.BladeAttackWidth);

            var vertices = new Vertices(bladeVertices);
            var shape = new PolygonShape(vertices, 1f);
            var parry = false;
            var atk = PlayerAttack(
                shape,
                player.GameObject.Transform.Position,
                direction.Angle(),
                async _ => Console.WriteLine("hit"),
                _ => true,
                async _ =>
                {
                    parry = true;
                    Console.WriteLine("parry");
                    Intend(player.GameObject, new SlideState(player, -direction, 20, 0.05f));
                });
            
            atk.Start();
            
            await NextFrame();
            atk.End();
            if (!parry)
            {
                SolsDawn.AnimationsPool.Add(
                    new ArrowPentagonTraceAnimation(
                        player.Stats.BladeTraceDuration,
                        player.GameObject.Transform.Position,
                        direction,
                        player.Stats.BladeDashDistance,
                        player.Stats.BladeDashWidth,
                        player.Stats.BladeAttackDistance,
                        player.Stats.BladeAttackWidth,
                        player.Stats.BladeTraceStartColor
                    ));

                player.GameObject.Transform.Position += direction * player.Stats.BladeDashDistance;
            }

            Intend(player.GameObject, new IdleState(player));
        }
    }

    public class SlideState(
        Player player,
        Vector2 direction,
        float speed,
        float time)
        : State
    {
        public override async Task Update()
        {
            var startTime = Time.TotalGameTime.TotalSeconds;
            while (Time.TotalGameTime.TotalSeconds - startTime < time)
            {
                player.GameObject.Transform.Position += direction * speed * ElapsedSeconds;
                await NextFrame();
            }
            
            Intend(player.GameObject, new IdleState(player));
        }
    }

    /*
    public class FireExecuteState(
        Player player,
        Vector2 lookPosition,
        IReadOnlyList<GameObject> targets)
        : State
    {
        public readonly IReadOnlyList<GameObject> Targets = targets;
        public readonly Vector2 LookPosition = lookPosition;

        public override void Enter(State from)
        {
            var direction = LookPosition - player.GameObject.Transform.Position;
            direction.Normalize();
            var fireEnd = player.GameObject.Transform.Position + direction * player.Stats.FireDistance;

            Game.AnimationsPool.Add(new LineTrace(
                new Transform2 { Position = player.GameObject.Transform.Position },
                fireEnd,
                player.Stats.FireTraceWidth,
                player.Stats.FireTraceDuration,
                player.Stats.FireTraceStartColor,
                player.Stats.FireTraceEndColor));

            if (Targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(player.GameObject, Targets, 1));
            }

            var pending = new IdleState(player);
            IntentionsPool.AddIntention(Intend(player.GameObject, pending));
        }
    }*/
}