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

public class BossStats
{
    public Color Color;
    [Units] public float Width;
    [Units] public float Height;

    public float HitDuration;
    public Color HitBlinkColor;
    
    public float BladeTelegraphDuration;
    public Color BladeTelegraphBlinkColor;
    [Units] public float BladeTelegraphStarDistance;
    public float BladeTelegraphStarDuration;
    public Color BladeTelegraphStarColor;
    [Units] public float BladeTelegraphStarOuterRadius;
    [Units] public float BladeTelegraphStarInnerRadius;
    [Euler] public float BladeTelegraphStarStartAngle;
    [Euler] public float BladeTelegraphStarDeltaAngle;
    [Units] public float BladeTelegraphStarThickness;
    
    [Units] public float BladeAttackDistance;
    [Euler] public float BladeAttackEdgeAngle;
    [Units] public float BladeAttackEdgeLength;
    [Units] public float BladeAttackEdgeWidth;
    [Units] public float BladeDashDistance;
    [Units] public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;
    public Color BladeTraceEndColor;

    public float BladeParriedDuration;
    public Color BladeParriedColor;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireTelegraphDuration;
    public Color FireTelegraphBlinkColor;

    [Units] public float FireDistance;
    [Units] public float FireWidth;
    public float FireTraceDuration;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;
    
    public float FireParriedDuration;
    public Color FireParriedColor;
    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;
    
    [Units] public float TeleportTraceWidth;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Boss : Component<Boss>, IUpdatable 
{
    public readonly BossStats Stats;
    
    public State CurrentState => _machine.State;
    public enum State 
    { 
        Pending, 
        Idling, 
        Teleporting, 
        BladeTelegraphing, BladeAttacking, BladeParried, 
        FireTelegraphing, FireAttacking, FireParried 
    }
    private readonly StateMachine<State, State> _machine;
    private double _actionStartTime;
    private double _actionDuration;
    private Vector2 _actionLookPosition;
    private GameObject _parryGO;
    
    private readonly Collider _collider;
    private readonly Hp _hp;
    private readonly BossAnimations _animations;
    
    public Boss(GameObject go) : base(go)
    {
        Stats = MainConfig.BossStats; 
            
        _collider = go.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();     
        _hp = go.GetComponent<Hp>() ?? throw new ComponentNotFoundException<Hp>();
        var animator = go.GetComponent<Animator<BossAnimations>>() ?? throw new ComponentNotFoundException<Animator<BossAnimations>>();
        _animations = animator.Player;
        
        _machine = new StateMachine<State, State>(State.Pending);
        _machine.Configure(State.Pending)
            .Permit(State.Idling)
            .Permit(State.BladeTelegraphing)
            .Permit(State.FireTelegraphing)
            .Permit(State.Teleporting);
        _machine.Configure(State.Idling)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.Idle))
            .Permit(State.Pending);
        _machine.Configure(State.Teleporting)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.Idle))
            .Permit(State.Pending);
        
        _machine.Configure(State.BladeTelegraphing)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.BladeTelegraph))
            .OnExit(RemoveTelegraphBladeCollider)
            .Permit(State.BladeAttacking)
            .Permit(State.BladeParried);
        _machine.Configure(State.BladeAttacking)
            .Permit(State.Pending);
        _machine.Configure(State.BladeParried)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.BladeParried))
            .Permit(State.Pending);
        
        _machine.Configure(State.FireTelegraphing)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.FireTelegraph))
            .OnExit(_ => GameObject.RemoveComponent<Parry>())
            .Permit(State.FireAttacking)
            .Permit(State.FireParried);
        _machine.Configure(State.FireAttacking)
            .Permit(State.Pending);
        _machine.Configure(State.FireParried)
            .OnEntry(_ => _animations.TryPlay(BossAnimations.FireParried))
            .Permit(State.Pending);
    }

    public override void Dispose()
    {
    }
    
    public void Update()
    {
        var timeExpired = Time.TotalGameTime.TotalSeconds - _actionStartTime > _actionDuration;
        switch (_machine.State)
        {
            case State.Idling:
            case State.BladeParried:
            case State.FireParried:
                if (timeExpired)
                    _machine.Fire(State.Pending);
                break;
            
            case State.BladeTelegraphing:
                if (timeExpired)
                    IntendBlade(_actionLookPosition);
                break;
            
            case State.FireTelegraphing:
                if (timeExpired)
                    IntendFire(_actionLookPosition);
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
        Console.WriteLine($"[Boss] Damaged : {value}");
        _animations.TryPlay(BossAnimations.Hit);
    }
    
    // ACTIONS
    
    public void Wait(double time)
    {
        _actionDuration = time;
        _actionStartTime = Time.TotalGameTime.TotalSeconds; //TODO it is bad, because long game time has less accuracy
        _machine.Fire(State.Idling);
    }
    
    public void Teleport(Vector2 position)
    {
        _machine.Fire(State.Teleporting);
        Game.EffectsPool.Add(new LineTrace(
            2,
            GameObject.Transform.Position,
            position,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceWidth
            ));
        
        GameObject.Transform.Position = position;
        _machine.Fire(State.Pending);
    }

    public void Blade(Vector2 lookPosition)
    {
        _actionLookPosition = lookPosition;
        _actionStartTime = Time.TotalGameTime.TotalSeconds; //TODO it is bad, because long game time has less accuracy
        _actionDuration = Stats.BladeTelegraphDuration;
        CreateTelegraphBladeCollider(lookPosition);
        _animations.LookPosition = lookPosition;
        _machine.Fire(State.BladeTelegraphing);
    }

    public void Fire(Vector2 lookPosition)
    {
        _actionLookPosition = lookPosition;
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.FireTelegraphDuration;
        new Parry(GameObject, GameObject, ParryType.Fire);
        _machine.Fire(State.FireTelegraphing);
    }
    
    // CREATES INTENTION

    private void IntendBlade(Vector2 lookPosition)
    {
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
        Collision.Overlap(shape, Collision.LayerName.Player, targets);
        IntentionsPool.Add(new BladeAttackIntention(GameObject, targets, 1, lookPosition));
    }
    
    public void IntendFire(Vector2 lookPosition)
    {
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
        Collision.Overlap(shape, Collision.LayerName.Player, targets);
        IntentionsPool.Add(new FireAttackIntention(GameObject, targets, 1, lookPosition));
    }

    // USED BY INTENTION
 
    public void DoBlade(Vector2 lookPosition, IReadOnlyList<GameObject> targets)
    {
        _machine.Fire(State.BladeAttacking);
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

        _machine.Fire(State.Pending);
    }   
    
    public void ParryBlade()
    {
        var direction = _actionLookPosition - GameObject.Transform.Position;
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
        
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.BladeParriedDuration;
        _machine.Fire(State.BladeParried);
    }

    public void DoFire(Vector2 lookPosition, IReadOnlyList<GameObject> targets)
    {
        _machine.Fire(State.FireAttacking);
        var direction = _actionLookPosition - GameObject.Transform.Position;
        direction.Normalize();
         
        
        Game.EffectsPool.Add(new LineTrace(
            Stats.FireTraceDuration,
            GameObject.Transform.Position,
            GameObject.Transform.Position + direction * Stats.FireDistance,
            Stats.FireTraceStartColor,
            Stats.FireTraceEndColor,
            Stats.FireWidth));
        
        if (targets.Count > 0)
        {
            AffectsPool.Add(new DamageAffect(GameObject, targets, 1));
        }
        
        _machine.Fire(State.Pending);
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
        
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.FireParriedDuration;
        _machine.Fire(State.FireParried);
    }
    
    // INTERNAL

    private void CreateTelegraphBladeCollider(Vector2 lookPosition)
    {
        var bladeDirection = lookPosition - GameObject.Transform.Position;
        bladeDirection.Normalize();
        var bladeVertices = Helper.ArchVertices(
            GameObject.Transform.Position,
            bladeDirection,
            Stats.BladeDashDistance + Stats.BladeAttackDistance,
            Stats.Width,
            Stats.BladeAttackEdgeAngle,
            Stats.BladeAttackEdgeLength);

        _parryGO = new GameObject();
        var parryPolygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        new Collider(_parryGO, 10, Collision.LayerName.Parry, new CollisionShape2D(parryPolygon));
        new Parry(_parryGO, GameObject, ParryType.Blade);
    }

    private void RemoveTelegraphBladeCollider()
    {
        _parryGO.Dispose();
        _parryGO = null;
    }
}