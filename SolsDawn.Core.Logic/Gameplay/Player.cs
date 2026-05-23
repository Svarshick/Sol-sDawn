using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Effects;
using SolsDawn.Core.Logic.Gameplay.Animations;
using SolsDawn.Core.Logic.Gameplay.Interaction;
using Stateless;

namespace SolsDawn.Core.Logic.Gameplay;

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
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Player : Component<Player>, IUpdatable 
{
    public readonly PlayerStats Stats;
    
    public enum State { Idling, Moving, TeleportAiming }
    private readonly StateMachine<State, State> _machine;
    public State CurrentState => _machine.State;
    
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
    private double _lastTeleportUsage;
    private double _lastBladeUsage;
    private double _lastFireUsage;
    public bool TeleportCharged => Time.TotalGameTime.TotalSeconds - _lastTeleportUsage > Stats.TeleportRechargeDuration;
    public bool BladeCharged => Time.TotalGameTime.TotalSeconds - _lastBladeUsage > Stats.BladeRechargeDuration;
    public bool FireCharged => Time.TotalGameTime.TotalSeconds - _lastFireUsage > Stats.FireRechargeDuration;

    private Line _teleportLine;
    
    private readonly Collider _collider;
    private readonly Input _input;
    private readonly PlayerAnimations _animations;

    public Player(GameObject go, Input input) : base(go)
    {
        _input = input;

        Stats = MainConfig.PlayerStats;
        
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        var animator = go.GetComponent<Animator<PlayerAnimations>>() ?? throw new ComponentNotFoundException<Animator<PlayerAnimations>>();
        _animations = animator.Player;

        _machine = new StateMachine<State, State>(State.Idling);

        _machine.Configure(State.Idling)
            .Permit(State.Moving)
            .Permit(State.TeleportAiming);

        _machine.Configure(State.Moving)
            .Permit(State.Idling)
            .Permit(State.TeleportAiming);

        _machine.Configure(State.TeleportAiming)
            .Permit(State.Idling);

        _input.Move += Move;
        _input.TeleportStarted += TeleportStarted;
        _input.TeleportUpdated += TeleportUpdated;
        _input.TeleportReleased += TeleportReleased;
        _input.Blade += IntendBlade;
        _input.Fire += IntendFire;
    }

    public override void Dispose()
    {
        _input.Move -= Move;
        _input.TeleportStarted -= TeleportStarted;
        _input.TeleportUpdated -= TeleportUpdated;
        _input.TeleportReleased -= TeleportReleased;
        _input.Blade -= IntendBlade;
        _input.Fire -= IntendFire;
    }

    public void Update()
    {
        switch (_machine.State)
        {
            case (State.Idling):
                if (MoveDirection != Vector2.Zero)
                    _machine.Fire(State.Moving);
                break;
            case (State.Moving):
                if (MoveDirection == Vector2.Zero)
                {
                    _machine.Fire(State.Idling);
                    break;
                }

                var velocity = Stats.Velocity;
                var shift = velocity * (float)Time.ElapsedGameTime.TotalSeconds;
                GameObject.Transform.Position += shift * MoveDirection;
                break;
        }
        
        var bounds = BoundingBox2D.CreateFromCenterAndExtents(GameObject.Transform.Position, new Vector2(Stats.Width/2, Stats.Height/2));
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate()
    {
    }
    
    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Player] Damaged : {value}");
        _animations.TryPlay(PlayerAnimations.Hit);
    }
    
    private void TeleportStarted(Vector2 screenPosition)
    {
        if (!TeleportCharged)
            return;
        
        _machine.Fire(State.TeleportAiming);
        var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var endPosition = TeleportPosition(mousePosition, 0);
        _teleportLine = new(GameObject.Transform.Position, endPosition, Stats.TeleportStartColor, Stats.TeleportWidth);
        Game.EffectsPool.Add(_teleportLine);
    }

    private void TeleportUpdated(Vector2 screenPosition, double elapsedTime)
    {
        if (_machine.State != State.TeleportAiming)
            return;
        _teleportLine.Start = GameObject.Transform.Position;
        var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var lerp = TeleportLerp(elapsedTime);
        _teleportLine.End = TeleportPosition(mousePosition, lerp);
        _teleportLine.Color = Color.Lerp(Stats.TeleportStartColor, Stats.TeleportEndColor, lerp);
    }

    private void TeleportReleased(Vector2 screenPosition, double elapsedTime)
    {
        if (_machine.State != State.TeleportAiming)
            return;
        
        _teleportLine.IsFinished = true;
        _teleportLine = null;
        
        var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var lerp = TeleportLerp(elapsedTime);
        var endPosition = TeleportPosition(mousePosition, lerp);
        
        Game.EffectsPool.Add(new LineTrace(
            2,
            GameObject.Transform.Position,
            endPosition,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceWidth
            ));
        
        GameObject.Transform.Position = endPosition;
        _lastTeleportUsage = Time.TotalGameTime.TotalSeconds;
        _machine.Fire(State.Idling);
    }
    
    private float TeleportLerp(double elapsedTime)
    {
        return MathHelper.Clamp((float)(elapsedTime / Stats.TeleportHoldDuration), 0f, 1f);
    }
    
    private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
    {
        var teleportDirection = pointPosition - GameObject.Transform.Position;
        teleportDirection.Normalize();
        var delta = lerp * (Stats.TeleportMaxDistance - Stats.TeleportMinDistance);
        return GameObject.Transform.Position + teleportDirection * (Stats.TeleportMinDistance + delta);
    }
    
    private void Move(Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
    }

    private void IntendBlade(Vector2 screenPosition)
    {
        if (!BladeCharged)
            return;
        _lastBladeUsage = Time.TotalGameTime.TotalSeconds;
        
        var lookPosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var bladeDirection = lookPosition - GameObject.Transform.Position;
        bladeDirection.Normalize();
        var bladeVertices = Helper.ArchVertices(
            GameObject.Transform.Position,
            bladeDirection,
            Stats.BladeDashDistance + Stats.BladeAttackDistance,
            Stats.Width,
            Stats.BladeAttackEdgeAngle,
            Stats.BladeAttackEdgeLength);
        
        var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        var shape = new CollisionShape2D(polygon);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Enemy, targets);
        Collision.Overlap(shape, Collision.LayerName.Parry, targets);
        IntentionsPool.Add(new BladeAttackIntention(GameObject, targets, 1, lookPosition));
    }

    public void DoBlade(Vector2 lookPosition, IReadOnlyList<GameObject> targets)
    {
        var direction = lookPosition - GameObject.Transform.Position;
        direction.Normalize();
             
        Helper.DrawDashAttack(
            GameObject.Transform.Position,
            direction,
            Stats.BladeDashDistance,
            Stats.BladeDashWidth,
            Stats.BladeAttackDistance,
            Stats.BladeAttackEdgeAngle,
            Stats.BladeAttackEdgeLength,
            Stats.BladeAttackEdgeWidth,
            Stats.BladeTraceDuration,
            Stats.BladeTraceStartColor,
            Stats.BladeTraceEndColor);
        
        GameObject.Transform.Position += direction * Stats.BladeDashDistance;
        
        if (targets.Count > 0)
        {
            AffectsPool.Add(new DamageAffect(GameObject, targets, 1));
        }
    }

    public void ParryBlade(Vector2 lookPosition)
    {
        var direction = lookPosition - GameObject.Transform.Position;
        direction.Normalize();

        Helper.DrawDashAttack(
            GameObject.Transform.Position,
            direction,
            Stats.BladeDashDistance,
            Stats.BladeDashWidth,
            Stats.BladeAttackDistance,
            Stats.BladeAttackEdgeAngle,
            Stats.BladeAttackEdgeLength,
            Stats.BladeAttackEdgeWidth,
            Stats.BladeParryTraceDuration,
            Stats.BladeParryTraceStartColor,
            Stats.BladeParryTraceEndColor);
        
        GameObject.Transform.Position += direction * Stats.BladeDashDistance;
    }

    private void IntendFire(Vector2 screenPosition)
    {
        if (!FireCharged)
            return;
        _lastFireUsage = Time.TotalGameTime.TotalSeconds;
        
        var lookPosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var direction = lookPosition - GameObject.Transform.Position;
        direction.Normalize();
        var fireEnd = GameObject.Transform.Position + direction * Stats.FireDistance;
        
        var bounds = new OrientedBoundingBox2D(
            (GameObject.Transform.Position + fireEnd) / 2, 
            direction, 
            direction.PerpendicularClockwise(), 
            new Vector2(Stats.FireDistance/2, Stats.FireWidth/2));
        var shape = new CollisionShape2D(bounds);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Enemy, targets);
        IntentionsPool.Add(new FireAttackIntention(GameObject, targets, 1, lookPosition));
    }
    
    public void DoFire(Vector2 lookPosition, IReadOnlyList<GameObject> targets)
    {
        var direction = lookPosition - GameObject.Transform.Position;
        direction.Normalize();
        var fireEnd = GameObject.Transform.Position + direction * Stats.FireDistance;

        Game.EffectsPool.Add(new LineTrace(
            Stats.FireTraceDuration, 
            GameObject.Transform.Position, 
            fireEnd, 
            Stats.FireTraceStartColor, 
            Stats.FireTraceEndColor, 
            Stats.FireTraceWidth));

        if (targets.Count > 0)
        {
            AffectsPool.Add(new DamageAffect(GameObject, targets, 1));
        }
    }
    
    public void ParryFire(Vector2 parryPosition)
    {
        Game.EffectsPool.Add(new LineTrace(
            Stats.FireParryTraceDuration,
            GameObject.Transform.Position,
            parryPosition,
            Stats.FireParryTraceStartColor,
            Stats.FireParryTraceEndColor,
            Stats.FireWidth));
    }
}