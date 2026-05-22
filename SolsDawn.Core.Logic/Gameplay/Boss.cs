using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public readonly DebugStats DebugStats;
    
    public State CurrentState => _machine.State;
    public enum State 
    { 
        Pending, 
        Idling, 
        Teleporting, 
        BladeTelegraphing, BladeAttacking, BladeParried, 
        FireTelegraphing, FireAttacking, FireParried 
    }

    private enum Trigger
    {
        ActionFinished, Wait, 
        Teleport, 
        TelegraphBladeAttack, ExecuteBladeAttack, ParryBlade,
        TelegraphFireAttack, ExecuteFireAttack, ParryFire
    }
    private readonly StateMachine<State, Trigger> _machine;
    private double _actionStartTime;
    private double _actionDuration;
    private Vector2 _actionLookPosition;
    private GameObject _parryGO;
    
    private readonly SpriteBatch _spriteBatch;
    private readonly EffectsPool _effectsPool;
    private readonly ScreenLayout _layout;
    private readonly Collider _collider;
    private readonly Hp _hp;
    private readonly Animator _animator;
    
    public Boss(
        GameObject go,
        SpriteBatch spriteBatch,
        EffectsPool effectsPool,
        ScreenLayout layout) : base(go)
    {
        _spriteBatch = spriteBatch;
        _effectsPool = effectsPool;
        _layout = layout;
        
        Stats = ConfigReader.Read(MainConfig.BossStats, _layout);
        DebugStats = ConfigReader.Read(MainConfig.DebugStats, _layout);
            
        _collider = go.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();     
        _hp = go.GetComponent<Hp>() ?? throw new ComponentNotFoundException<Hp>();
        _animator = go.GetComponent<Animator>() ?? throw new ComponentNotFoundException<Animator>();
        
        _machine = new StateMachine<State, Trigger>(State.Pending);
        _machine.Configure(State.Pending)
            .Permit(Trigger.Wait, State.Idling)
            .Permit(Trigger.TelegraphBladeAttack, State.BladeTelegraphing)
            .Permit(Trigger.TelegraphFireAttack, State.FireTelegraphing)
            .Permit(Trigger.Teleport, State.Teleporting);
        _machine.Configure(State.Idling)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Idle))
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.Teleporting)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Idle))
            .Permit(Trigger.ActionFinished, State.Pending);
        
        _machine.Configure(State.BladeTelegraphing)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.BladeTelegraph))
            .OnExit(RemoveTelegraphBladeCollider)
            .Permit(Trigger.ExecuteBladeAttack, State.BladeAttacking)
            .Permit(Trigger.ParryBlade, State.BladeParried);
        _machine.Configure(State.BladeAttacking)
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.BladeParried)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.BladeParried))
            .Permit(Trigger.ActionFinished, State.Pending);
        
        _machine.Configure(State.FireTelegraphing)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.FireTelegraph))
            .OnExit(_ => GameObject.RemoveComponent<Parry>())
            .Permit(Trigger.ExecuteFireAttack, State.FireAttacking)
            .Permit(Trigger.ParryFire, State.FireParried);
        _machine.Configure(State.FireAttacking)
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.FireParried)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.FireParried))
            .Permit(Trigger.ActionFinished, State.Pending);
    }

    public override void Dispose()
    {
    }
    
    public void Update(GameTime gameTime)
    {
        var timeExpired = gameTime.TotalGameTime.TotalSeconds - _actionStartTime > _actionDuration;
        switch (_machine.State)
        {
            case State.Idling:
            case State.BladeParried:
            case State.FireParried:
                if (timeExpired)
                    _machine.Fire(Trigger.ActionFinished);
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
    
    public void LateUpdate(GameTime gameTime)
    {
    }
    
    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Boss] Damaged : {value}");
        _animator.TryPlay(BossAnimations.Hit);
    }
    
    // ACTIONS
    
    public void Wait(double time)
    {
        _actionDuration = time;
        _actionStartTime = Time.TotalGameTime.TotalSeconds; //TODO it is bad, because long game time has less accuracy
        _machine.Fire(Trigger.Wait);
    }
    
    public void Teleport(Vector2 position)
    {
        _machine.Fire(Trigger.Teleport);
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            2,
            GameObject.Transform.Position,
            position,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceWidth
            ));
        
        GameObject.Transform.Position = position;
        _machine.Fire(Trigger.ActionFinished);
    }

    public void Blade(Vector2 lookPosition)
    {
        _actionLookPosition = lookPosition;
        _actionStartTime = Time.TotalGameTime.TotalSeconds; //TODO it is bad, because long game time has less accuracy
        _actionDuration = Stats.BladeTelegraphDuration;
        CreateTelegraphBladeCollider(lookPosition);
        _machine.Fire(Trigger.TelegraphBladeAttack);
    }

    public void Fire(Vector2 lookPosition)
    {
        _actionLookPosition = lookPosition;
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.FireTelegraphDuration;
        new Parry(GameObject, GameObject, ParryType.Fire);
        _machine.Fire(Trigger.TelegraphFireAttack);
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
        _machine.Fire(Trigger.ExecuteBladeAttack);
        var direction = lookPosition - GameObject.Transform.Position;
        direction.Normalize();
        
        Helper.DrawDashAttack(
            _effectsPool,
            _spriteBatch,
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

        _machine.Fire(Trigger.ActionFinished);
    }   
    
    public void ParryBlade()
    {
        var direction = _actionLookPosition - GameObject.Transform.Position;
        direction.Normalize();
         
        Helper.DrawDashAttack(
            _effectsPool,
            _spriteBatch,
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
        _machine.Fire(Trigger.ParryBlade);
    }

    public void DoFire(Vector2 lookPosition, IReadOnlyList<GameObject> targets)
    {
        _machine.Fire(Trigger.ExecuteFireAttack);
        var direction = _actionLookPosition - GameObject.Transform.Position;
        direction.Normalize();
         
        
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
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
        
        _machine.Fire(Trigger.ActionFinished);
    }

    public void ParryFire(Vector2 parryPosition)
    {
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            Stats.FireParryTraceDuration,
            GameObject.Transform.Position,
            parryPosition,
            Stats.FireParryTraceStartColor,
            Stats.FireParryTraceEndColor,
            Stats.FireWidth));
        
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.FireParriedDuration;
        _machine.Fire(Trigger.ParryFire);
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