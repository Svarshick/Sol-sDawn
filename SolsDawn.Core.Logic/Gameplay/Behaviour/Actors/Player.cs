using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class PlayerStats
{
    public Color Color;
    [Units] public float Width;
    [Units] public float Height;
    [Units] public float Velocity;
    [Units] public float CursorRadius;
    public Color CursorColor;

    public float TeleportRechargeDuration;
    public float BladeRechargeDuration;
    public float FireRechargeDuration;
    
    public float HitInvulnerabilityDuration;
    public Color HitBlinkColor;
    
    [Units] public float BladeAttackDistance;
    [Euler] public float BladeAttackEdgeAngle;
    [Units] public float BladeAttackEdgeLength;
    [Units] public float BladeAttackEdgeWidth;
    [Units] public float BladeDashDistance;
    [Units] public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;
    public Color BladeTraceEndColor;

    //[Euler] public float BladeParryAngle;
    [Units] public float BladeParryPushDistance;
    [Units] public float BladeParryPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;
    

    [Units] public float FireDistance;
    [Units] public float FireWidth;
    public float FireTraceDuration;
    [Units] public float FireTraceWidth;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;
    
    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;
    
    [Units] public float TeleportMinDistance;
    [Units] public float TeleportMaxDistance;
    public float TeleportHoldDuration;
    [Units] public float TeleportWidth;
    public Color TeleportStartColor;
    public Color TeleportEndColor;
    [Units] public float TeleportTraceWidth;
    public float TeleportTraceDuration;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Player : Component<Player>, IUpdatable
{
    public readonly PlayerStats Stats;
    
    public State State { get; private set; }

    public double LastTeleportUsage;
    public double LastBladeUsage;
    public double LastFireUsage;

    public bool TeleportCharged => Time.TotalGameTime.TotalSeconds - LastTeleportUsage > Stats.TeleportRechargeDuration;
    public bool BladeCharged => Time.TotalGameTime.TotalSeconds - LastBladeUsage > Stats.BladeRechargeDuration;
    public bool FireCharged => Time.TotalGameTime.TotalSeconds - LastFireUsage > Stats.FireRechargeDuration;

    private readonly Collider _collider;
    private readonly PlayerAnimations _animations;
    
    public Player(GameObject go) : base(go)
    {
        Stats = MainConfig.PlayerStats;

        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        var animator = go.GetComponent<Animator<PlayerAnimations>>() ?? throw new ComponentNotFoundException<Animator<PlayerAnimations>>();
        _animations = animator.Player;

        State = new IdleState(this);
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

        public override void Update()
        {
            Direction = input.Move;
            player.GameObject.Transform.Position += 
                Direction * player.Stats.Velocity * (float)Time.ElapsedGameTime.TotalSeconds;
        }
    }

    public class TeleportState(
        Player player,
        Input input)
        : State
    {
        public Vector2 ScreenPosition = input.Teleport.ScreenPosition;
        public double ElapsedTime;

        private LineIdle _teleportLine;

        public override void Enter(State from)
        {
            ScreenPosition = input.Teleport.ScreenPosition;
            var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(ScreenPosition);
            var endPosition = TeleportPosition(mousePosition, 0);
            _teleportLine = new(player.GameObject.Transform, endPosition, player.Stats.TeleportWidth, player.Stats.TeleportStartColor);
            Game.AnimationsPool.Add(_teleportLine);
        }
        
        public override void Update()
        {
            ScreenPosition = input.Teleport.ScreenPosition;
            ElapsedTime += Time.ElapsedGameTime.TotalSeconds;
            var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(ScreenPosition);
            var lerp = TeleportLerp(ElapsedTime);
            _teleportLine.End = TeleportPosition(mousePosition, lerp);
            _teleportLine.Color = Color.Lerp(player.Stats.TeleportStartColor, player.Stats.TeleportEndColor, lerp);
        }

        public override void Exit(State to)
        {
            ScreenPosition = input.Teleport.ScreenPosition;
            _teleportLine.IsFinished = true;
            var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(ScreenPosition);
            var lerp = TeleportLerp(ElapsedTime);
            var endPosition = TeleportPosition(mousePosition, lerp);

            Game.AnimationsPool.Add(new LineTrace(
                new Transform { Position = player.GameObject.Transform.Position },
                endPosition,
                player.Stats.TeleportTraceWidth,
                player.Stats.TeleportTraceDuration,
                player.Stats.TeleportTraceStartColor,
                player.Stats.TeleportTraceEndColor));

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

    public class BladeExecuteState(
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

            Helper.DrawDashAttack(
                player.GameObject.Transform.Position,
                direction,
                player.Stats.BladeDashDistance,
                player.Stats.BladeDashWidth,
                player.Stats.BladeAttackDistance,
                player.Stats.BladeAttackEdgeAngle,
                player.Stats.BladeAttackEdgeLength,
                player.Stats.BladeAttackEdgeWidth,
                player.Stats.BladeTraceDuration,
                player.Stats.BladeTraceStartColor,
                player.Stats.BladeTraceEndColor);

            player.GameObject.Transform.Position += direction * player.Stats.BladeDashDistance;

            if (Targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(player.GameObject, Targets, 1));
            }
            
            var pending = new IdleState(player);
            IntentionsPool.Add(Intend(player.GameObject, pending));
        }
    }

    public class BladeParryState(
        Player player,
        Vector2 endPosition)
        : State
    {
        public Vector2 PushPosition;
        
        public override void Enter(State from)
        {
            var crash = endPosition - player.GameObject.Transform.Position;
            var crashDistance = crash.Length();
            var direction = Vector2.Normalize(crash);

            var dashDistance = crashDistance > player.Stats.BladeDashDistance
                ? player.Stats.BladeDashDistance
                : 0;

            var bladeDistance = crashDistance > player.Stats.BladeDashDistance
                ? crashDistance - player.Stats.BladeDashDistance
                : crashDistance;

            Helper.DrawDashAttack(
                player.GameObject.Transform.Position,
                direction,
                dashDistance,
                player.Stats.BladeDashWidth,
                bladeDistance,
                player.Stats.BladeAttackEdgeAngle,
                player.Stats.BladeAttackEdgeLength,
                player.Stats.BladeAttackEdgeWidth,
                player.Stats.BladeParryTraceDuration,
                player.Stats.BladeParryTraceStartColor,
                player.Stats.BladeParryTraceEndColor);

            if (dashDistance > 0)
            {
                player.GameObject.Transform.Position += direction * dashDistance;
            }
            
            PushPosition = player.GameObject.Transform.Position - direction * player.Stats.BladeParryPushDistance;
        }

        public override void Update()
        {
            if (player.GameObject.Transform.Position != PushPosition)
            {
                var remain = PushPosition - player.GameObject.Transform.Position;
                var direction = Vector2.Normalize(remain);
                var delta = direction * (float)(Time.ElapsedGameTime.TotalSeconds * player.Stats.BladeParryPushVelocity);
                if (delta.LengthSquared() >= remain.LengthSquared())
                {
                    player.GameObject.Transform.Position = PushPosition;
                }
                else
                {
                    player.GameObject.Transform.Position += delta;
                }
                return;
            }
            
            var pending = new IdleState(player);
            IntentionsPool.Add(Intend(player.GameObject, pending));
        }
    }

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
                new Transform { Position = player.GameObject.Transform.Position },
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
            IntentionsPool.Add(Intend(player.GameObject, pending));
        }
    }

    public class FireParryState(
        Player player,
        Vector2 parryPosition)
        : State
    {
        public override void Enter(State from)
        {
            Game.AnimationsPool.Add(new LineTrace(
                new Transform { Position = player.GameObject.Transform.Position },
                parryPosition,
                player.Stats.FireWidth,
                player.Stats.FireParryTraceDuration,
                player.Stats.FireParryTraceStartColor,
                player.Stats.FireParryTraceEndColor));
            
            var pending = new IdleState(player);
            IntentionsPool.Add(Intend(player.GameObject, pending));
        }
    }
}