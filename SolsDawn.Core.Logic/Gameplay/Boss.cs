using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Effects;
using SolsDawn.Core.Logic.Gameplay.Animations;
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
    public Color BladeTraceEndColor;
    public Color BladeTraceStartColor;

    public float ParryDuration;
    public Color ParryColor;
    
    [Units] public float TeleportTraceWidth;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Boss : Component<Boss>, IUpdatable 
{
    public readonly BossStats Stats;
    public readonly DebugStats DebugStats;
    
    public State CurrentState => _machine.State;
    public enum State { Pending, Idling, Teleporting, BladeTelegraphing, BladeAttacking, Parried }
    private enum Trigger { ActionFinished, Wait, Teleport, TelegraphBladeAttack, ExecuteBladeAttack, Parry }
    private readonly StateMachine<State, Trigger> _machine;
    private double _actionStartTime;
    private double _actionDuration;
    private Vector2 _lookPosition;
    private GameObject _parryGO;
    private IEffect _parryEffect;
    
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
            .Permit(Trigger.Teleport, State.Teleporting);
        _machine.Configure(State.Idling)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Idle))
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.Teleporting)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Idle))
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.BladeTelegraphing)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Telegraph))
            .OnEntry(TelegraphBladeEntry)
            .OnExit(TelegraphBladeExit)
            .Permit(Trigger.ExecuteBladeAttack, State.BladeAttacking)
            .Permit(Trigger.Parry, State.Parried);
        _machine.Configure(State.BladeAttacking)
            .OnEntry(BladeAttackEntry)
            .Permit(Trigger.ActionFinished, State.Pending);
        _machine.Configure(State.Parried)
            .OnEntry(_ => _animator.TryPlay(BossAnimations.Parried))
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
            case State.Parried:
                if (timeExpired)
                    _machine.Fire(Trigger.ActionFinished);
                break;
            
            case State.BladeTelegraphing:
                if (timeExpired)
                    _machine.Fire(Trigger.ExecuteBladeAttack);
                break;
        }
        
        var bounds = BoundingBox2D.CreateFromCenterAndExtents(GameObject.Transform.Position, new Vector2(Stats.Width/2, Stats.Height/2));
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate(GameTime gameTime)
    {
    }

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
        _lookPosition = lookPosition;
        _actionStartTime = Time.TotalGameTime.TotalSeconds; //TODO it is bad, because long game time has less accuracy
        _actionDuration = Stats.BladeTelegraphDuration;
        _machine.Fire(Trigger.TelegraphBladeAttack);
    }
    
    public void Parry()
    {
        _actionStartTime = Time.TotalGameTime.TotalSeconds;
        _actionDuration = Stats.ParryDuration;
        _machine.Fire(Trigger.Parry);
        Console.WriteLine("Parried");
    }

    private void TelegraphBladeEntry()
    {
        var bladeDirection = _lookPosition - GameObject.Transform.Position;
        bladeDirection.Normalize();
        var attackPosition = GameObject.Transform.Position + bladeDirection * (Stats.BladeDashDistance + Stats.BladeAttackDistance);
        var blade = new Vector2(0, -Stats.BladeAttackEdgeLength);
        Vector2[] bladeVertices =
        [
            GameObject.Transform.Position + bladeDirection.PerpendicularCounterClockwise() * Stats.Width / 2,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi - Stats.BladeAttackEdgeAngle / 2 + bladeDirection.ToAngle()),
            attackPosition,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi + Stats.BladeAttackEdgeAngle / 2 + bladeDirection.ToAngle()),
            GameObject.Transform.Position + bladeDirection.PerpendicularClockwise() * Stats.Width / 2
        ];
        #if DEBUG
        _parryEffect = new PolygonTrace(
            _spriteBatch, 
            Stats.BladeTraceDuration, 
            Vector2.Zero, 
            bladeVertices, 
            DebugStats.ParryColliderColor, 
            DebugStats.ParryColliderColor, 
            DebugStats.HitColliderWidth);
        _effectsPool.Add(_parryEffect);
        #endif
        
        _parryGO = new GameObject();
        var parryPolygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        new Collider(_parryGO, 10, Collision.LayerName.Parry, new CollisionShape2D(parryPolygon));
        new Parry(_parryGO, GameObject);
       
    }
    
    private void TelegraphBladeExit()
    {
        _parryEffect?.Cancel();
        _parryEffect = null;
        _parryGO.Dispose();
        _parryGO = null;
    }

    private void BladeAttackEntry()
    {
        var bladeDirection = _lookPosition - GameObject.Transform.Position;
        bladeDirection.Normalize();

        var nextPosition = GameObject.Transform.Position + bladeDirection * Stats.BladeDashDistance;
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            Stats.BladeTraceDuration,
           GameObject.Transform.Position,
            nextPosition,
            Stats.BladeTraceStartColor,
            Stats.BladeTraceEndColor,
            Stats.BladeDashWidth
            ));
        
        var attackPosition = nextPosition + bladeDirection * Stats.BladeAttackDistance;
        var blade = new Vector2(0, -Stats.BladeAttackEdgeLength);
        Vector2[] bladeVertices =
        [
            GameObject.Transform.Position + bladeDirection.PerpendicularCounterClockwise() * Stats.Width / 2,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi - Stats.BladeAttackEdgeAngle / 2 + bladeDirection.ToAngle()),
            attackPosition,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi + Stats.BladeAttackEdgeAngle / 2 + bladeDirection.ToAngle()),
            GameObject.Transform.Position + bladeDirection.PerpendicularClockwise() * Stats.Width / 2
        ];
        GameObject.Transform.Position = nextPosition;
        
        #if DEBUG
        _effectsPool.Add(new PolygonTrace(
            _spriteBatch, 
            Stats.BladeTraceDuration, 
            Vector2.Zero, 
            bladeVertices, 
            DebugStats.HitColliderColor, 
            DebugStats.HitColliderColor, 
            DebugStats.HitColliderWidth));
        #else
        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.BladeTraceDuration, bladeVertices[2], bladeVertices[1], Stats.BladeTraceStartColor, Stats.BladeTraceEndColor, Stats.BladeAttackEdgeWidth, 1));
        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.BladeTraceDuration, bladeVertices[2], bladeVertices[3], Stats.BladeTraceStartColor, Stats.BladeTraceEndColor, Stats.BladeAttackEdgeWidth, 1));
        #endif
        
        var bounds = BoundingBox2D.CreateFromPoints(bladeVertices);
        foreach (var actor in Collision.World.QueryCandidates(bounds, Collision.LayerName.Player))
        {
            var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
            var shape = new CollisionShape2D(polygon);
            if (shape.TryGetCollision(actor.Shape, out _) && actor is Collider collider)
            {
                var affect = new Affect(
                    GameObject,
                    collider.GameObject,
                    AffectType.Damage,
                    new DamageArgs(1));
                AffectResolver.Affect(affect);
            }
        }
        
        _machine.Fire(Trigger.ActionFinished);
    }

    public void Damage(int value)
    {
        _animator.TryPlay(BossAnimations.Hit);
    }
}