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

public class PlayerStats
{
    public Color Color;
    [Units] public float Width;
    [Units] public float Height;
    [Units] public float Velocity;

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
    [Units] public float BladeAimDistance;
    [Units] public float BladeAimRadius;
    public Color BladeAimColor;

    [Units] public float FireDistance;
    [Units] public float FireWidth;
    public float FireTraceDuration;
    [Units] public float FireTraceWidth;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;
    
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
    public readonly DebugStats DebugStats;
    
    public enum State { Idle, Moving, TeleportAiming, Attacking }
    private enum Trigger { Move, Stop, StartTeleport, ExecuteTeleport, Attack }
    private readonly StateMachine<State, Trigger> _machine;
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
    private readonly SpriteBatch _spriteBatch;
    private readonly EffectsPool _effectsPool;
    private readonly ScreenLayout _layout;
    private readonly Input _input;
    private readonly Animator _animator;

    public Player(
        GameObject go,
        SpriteBatch spriteBatch,
        EffectsPool effectsPool,
        ScreenLayout layout,
        Input input) : base(go)
    {
        _spriteBatch = spriteBatch;
        _effectsPool = effectsPool;
        _layout = layout;
        _input = input;

        Stats = ConfigReader.Read(MainConfig.PlayerStats, _layout);
        DebugStats = ConfigReader.Read(MainConfig.DebugStats, _layout);

        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _animator = go.GetComponent<Animator>() ?? throw new ComponentNotFoundException<Animator>();

        _machine = new StateMachine<State, Trigger>(State.Idle);

        _machine.Configure(State.Idle)
            .Permit(Trigger.Move, State.Moving)
            .Permit(Trigger.StartTeleport, State.TeleportAiming)
            .Permit(Trigger.Attack, State.Attacking);

        _machine.Configure(State.Moving)
            .Permit(Trigger.Stop, State.Idle)
            .Permit(Trigger.StartTeleport, State.TeleportAiming)
            .Permit(Trigger.Attack, State.Attacking);

        _machine.Configure(State.TeleportAiming)
            .Permit(Trigger.ExecuteTeleport, State.Idle);

        _machine.Configure(State.Attacking)
            .Permit(Trigger.Stop, State.Idle)
            .Permit(Trigger.Move, State.Moving)
            .Permit(Trigger.StartTeleport, State.TeleportAiming);

        _input.Move += Move;
        _input.TeleportStarted += TeleportStarted;
        _input.TeleportUpdated += TeleportUpdated;
        _input.TeleportReleased += TeleportReleased;
        _input.Blade += Blade;
        _input.Fire += Fire;
    }

    public override void Dispose()
    {
        _input.Move -= Move;
        _input.TeleportStarted -= TeleportStarted;
        _input.TeleportUpdated -= TeleportUpdated;
        _input.TeleportReleased -= TeleportReleased;
        _input.Blade -= Blade;
        _input.Fire -= Fire;
    }

    public void Update(GameTime gameTime)
    {
        switch (_machine.State)
        {
            case (State.Idle):
                if (MoveDirection != Vector2.Zero)
                    _machine.Fire(Trigger.Move);
                break;
            case (State.Moving):
                if (MoveDirection == Vector2.Zero)
                {
                    _machine.Fire(Trigger.Stop);
                    break;
                }

                var velocity = Stats.Velocity;
                var shift = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                GameObject.Transform.Position += shift * MoveDirection;
                break;
        }
        
        var bounds = BoundingBox2D.CreateFromCenterAndExtents(GameObject.Transform.Position, new Vector2(Stats.Width/2, Stats.Height/2));
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate(GameTime gameTime)
    {
    }
    
    private void TeleportStarted(Vector2 screenPosition)
    {
        bool teleportLock = Time.TotalGameTime.TotalSeconds - _lastTeleportUsage < Stats.TeleportRechargeDuration;
        if (teleportLock)
            return;
        
        _machine.Fire(Trigger.StartTeleport);
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var endPosition = TeleportPosition(mousePosition, 0);
        _teleportLine = new(_spriteBatch, GameObject.Transform.Position, endPosition, Stats.TeleportStartColor, Stats.TeleportWidth);
        _effectsPool.Add(_teleportLine);
    }

    private void TeleportUpdated(Vector2 screenPosition, double elapsedTime)
    {
        if (_machine.State != State.TeleportAiming)
            return;
        _teleportLine.Start = GameObject.Transform.Position;
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
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
        
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var lerp = TeleportLerp(elapsedTime);
        var endPosition = TeleportPosition(mousePosition, lerp);
        
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            2,
            GameObject.Transform.Position,
            endPosition,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceWidth
            ));
        
        GameObject.Transform.Position = endPosition;
        _lastTeleportUsage = Time.TotalGameTime.TotalSeconds;
        _machine.Fire(Trigger.ExecuteTeleport);
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

    private void Blade(Vector2 screenPosition)
    {
        if (Time.TotalGameTime.TotalSeconds - _lastBladeUsage < Stats.BladeRechargeDuration)
            return;
        var worldPosition = _layout.Camera.ScreenToWorld(screenPosition);
        var bladeDirection = worldPosition - GameObject.Transform.Position;
        bladeDirection.Normalize();

        var nextPosition = GameObject.Transform.Position + bladeDirection * Stats.BladeDashDistance;
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            Stats.BladeTraceDuration,
            GameObject.Transform.Position,
            nextPosition,
            Stats.BladeTraceStartColor,
            Stats.BladeTraceEndColor,
            Stats.BladeDashWidth));
        
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
        var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        var shape = new CollisionShape2D(polygon);
        foreach (var actor in Collision.World.QueryCandidates(bounds, Collision.LayerName.Enemy))
        {
            if (shape.TryGetCollision(actor.Shape, out _)  && actor is Collider collider)
            {
                var affect = new Affect(
                    GameObject,
                    collider.GameObject,
                    AffectType.Damage,
                    new DamageArgs(1));
                AffectResolver.Affect(affect);
            }
        }
        
        foreach (var actor in Collision.World.QueryCandidates(bounds, Collision.LayerName.Parry))
        {
            if (shape.TryGetCollision(actor.Shape, out _)  && actor is Collider collider)
            {
                var parry = collider.GameObject.GetComponent<Parry>();
                if (parry is null)
                    continue;
                var affect = new Affect(
                    GameObject,
                    parry.Target,
                    AffectType.Parry,
                    new ParryArgs(parry));
                AffectResolver.Affect(affect);
            }
        }
        
        _lastBladeUsage = Time.TotalGameTime.TotalSeconds;
    }

    private void Fire(Vector2 screenPosition)
    {
        if (Time.TotalGameTime.TotalSeconds - _lastFireUsage < Stats.FireRechargeDuration)
            return;
        var worldPosition = _layout.Camera.ScreenToWorld(screenPosition);
        var fireDirection = worldPosition - GameObject.Transform.Position;
        fireDirection.Normalize();
        var fireEnd = GameObject.Transform.Position + fireDirection * Stats.FireDistance;

        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.FireTraceDuration, GameObject.Transform.Position, fireEnd, Stats.FireTraceStartColor, Stats.FireTraceEndColor, Stats.FireTraceWidth));
        
        var bounds = new OrientedBoundingBox2D(
            (GameObject.Transform.Position + fireEnd) / 2, 
            fireDirection, 
            new Vector2(fireDirection.Y, fireDirection.X), 
            new Vector2(Stats.FireDistance/2, Stats.FireWidth/2));
        foreach (var actor in Collision.World.QueryCandidates(BoundingBox2D.CreateFromPoints(bounds.GetCorners()), Collision.LayerName.Enemy))
        {
            var shape = new CollisionShape2D(bounds);
            if (shape.TryGetCollision(actor.Shape, out _)  && actor is Collider collider)
            {
                var affect = new Affect(
                    GameObject,
                    collider.GameObject,
                    AffectType.Damage,
                    new DamageArgs(1));
                AffectResolver.Affect(affect);
            }
        }
        
        _lastFireUsage = Time.TotalGameTime.TotalSeconds;
    }
    
    public void Damage(int value)
    {
        _animator.TryPlay(PlayerAnimations.Hit);
    }
}