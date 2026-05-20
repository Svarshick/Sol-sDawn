using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Effects;

namespace SolsDawn.Core.Logic.Gameplay;

public class PlayerStats
{
    public Color Color;
    [Units] public float Radius;
    [Units] public float Velocity;
    
    [Units] public float BladeAttackDistance;
    [Euler] public float BladeAttackAngle;
    [Units] public float BladeAttackLength;
    [Units] public float BladeAttackWidth;
    [Units] public float BladeDashDistance;
    [Units] public float BladeDashWidth;
    public float BladeTraceDuration;
    [Units] public float BladeAimDistance;
    [Units] public float BladeAimRadius;
    public Color BladeAimColor;

    [Units] public float FireDistance;
    [Units] public float FireWidth;
    public float FireTraceDuration;
    [Units] public float FireTraceWidth;
    public Color FireTraceColor;
    
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

public sealed class Player : Component<Player>, IUpdatable, IDrawable
{
    public readonly PlayerStats Stats;
    public readonly DebugStats DebugStats;
    
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
    private bool _moveDirectionUpdated;
    private bool _moveLock;

    private Line _teleportLine;
    
    private readonly Collider _collider;
    private readonly SpriteBatch _spriteBatch;
    private readonly EffectsPool _effectsPool;
    private readonly ScreenLayout _layout;
    private readonly Input _input;
    
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
        if (_moveDirectionUpdated)
        {
            var velocity = Stats.Velocity;
            var shift = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            GameObject.Position += shift * MoveDirection;
        }
        
        var bounds = new BoundingCircle2D(GameObject.Position, Stats.Radius);
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate(GameTime gameTime)
    {
        _moveDirectionUpdated = false;
    }
    
    public void Draw(GameTime gameTime)
    {
        var radius = Stats.Radius;
        _spriteBatch.DrawCircle(
            center: GameObject.Position,
            radius: radius,
            sides: 20,
            color: Stats.Color,
            thickness: radius 
        );

        var mouseState = MouseExtended.GetState();
        var mousePosition = _layout.Camera.ScreenToWorld(mouseState.Position.ToVector2());
        var bladeDirection = mousePosition - GameObject.Position;
        bladeDirection.Normalize();
        _spriteBatch.DrawCircle(
            center: GameObject.Position + bladeDirection * Stats.BladeAimDistance,
            radius: Stats.BladeAimRadius,
            sides: 20,
            color: Stats.BladeAimColor,
            thickness: Stats.BladeAimRadius
        );
    }
    
    private void TeleportStarted(Vector2 screenPosition)
    {
        _moveLock = true;
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var endPosition = TeleportPosition(mousePosition, 0);
        _teleportLine = new(_spriteBatch, GameObject.Position, endPosition, Stats.TeleportStartColor, Stats.TeleportWidth);
        _effectsPool.Add(_teleportLine);
    }

    private void TeleportUpdated(Vector2 screenPosition, double elapsedTime)
    {
        _teleportLine.Start = GameObject.Position;
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var lerp = TeleportLerp(elapsedTime);
        _teleportLine.End = TeleportPosition(mousePosition, lerp);
        _teleportLine.Color = Color.Lerp(Stats.TeleportStartColor, Stats.TeleportEndColor, lerp);
    }

    private void TeleportReleased(Vector2 screenPosition, double elapsedTime)
    {
        _teleportLine.IsFinished = true;
        _teleportLine = null;
        
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var lerp = TeleportLerp(elapsedTime);
        var endPosition = TeleportPosition(mousePosition, lerp);
        
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            2,
            GameObject.Position,
            endPosition,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceWidth
            ));
        
        GameObject.Position = endPosition;
        _moveLock = false;
    }
    
    private float TeleportLerp(double elapsedTime)
    {
        return MathHelper.Clamp((float)(elapsedTime / Stats.TeleportHoldDuration), 0f, 1f);
    }
    
    private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
    {
        var teleportDirection = pointPosition - GameObject.Position;
        teleportDirection.Normalize();
        var delta = lerp * (Stats.TeleportMaxDistance - Stats.TeleportMinDistance);
        return GameObject.Position + teleportDirection * (Stats.TeleportMinDistance + delta);
    }
    
    private void Move(Vector2 moveDirection)
    {
        if (_moveLock)
            return;
        MoveDirection = moveDirection;
        _moveDirectionUpdated = true;
    }

    private void Blade(Vector2 screenPosition)
    {
        var worldPosition = _layout.Camera.ScreenToWorld(screenPosition);
        var bladeDirection = worldPosition - GameObject.Position;
        bladeDirection.Normalize();

        var nextPosition = GameObject.Position + bladeDirection * Stats.BladeDashDistance;
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            Stats.BladeTraceDuration,
            GameObject.Position,
            nextPosition,
            Color.White,
            Color.Transparent,
            Stats.BladeDashWidth
            ));
        
        var attackPosition = nextPosition + bladeDirection * Stats.BladeAttackDistance;
        var blade = new Vector2(0, -Stats.BladeAttackLength);
        Vector2[] bladeVertices =
        [
            GameObject.Position + bladeDirection.PerpendicularCounterClockwise() * Stats.Radius,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi - Stats.BladeAttackAngle / 2 + bladeDirection.ToAngle()),
            attackPosition,
            attackPosition + Vector2.Rotate(blade, MathHelper.Pi + Stats.BladeAttackAngle / 2 + bladeDirection.ToAngle()),
            GameObject.Position + bladeDirection.PerpendicularClockwise() * Stats.Radius
        ];
        GameObject.Position = nextPosition;
        
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
        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.BladeTraceDuration, bladeVertices[2], bladeVertices[1], Color.White, Color.Transparent, Stats.BladeAttackWidth, 1));
        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.BladeTraceDuration, bladeVertices[2], bladeVertices[3], Color.White, Color.Transparent, Stats.BladeAttackWidth, 1));
        #endif
        
        var bounds = BoundingBox2D.CreateFromPoints(bladeVertices);
        var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        var shape = new CollisionShape2D(polygon);
        foreach (var actor in Collision.World.QueryCandidates(bounds, Collision.LayerName.Enemy))
        {
            if (actor.Shape.Intersects(shape) && actor is Collider collider)
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
            if (actor.Shape.Intersects(shape) && actor is Collider collider)
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
    }

    private void Fire(Vector2 screenPosition)
    {
        var worldPosition = _layout.Camera.ScreenToWorld(screenPosition);
        var fireDirection = worldPosition - GameObject.Position;
        fireDirection.Normalize();
        var fireEnd = GameObject.Position + fireDirection * Stats.FireDistance;

        _effectsPool.Add(new LineTrace(_spriteBatch, Stats.FireTraceDuration, GameObject.Position, fireEnd, Stats.FireTraceColor, Color.Transparent, Stats.FireTraceWidth));
        
        var bounds = new OrientedBoundingBox2D(
            (GameObject.Position + fireEnd) / 2, 
            fireDirection, 
            new Vector2(fireDirection.Y, fireDirection.X), 
            new Vector2(Stats.FireDistance/2, Stats.FireWidth/2));
        foreach (var actor in Collision.World.QueryCandidates(BoundingBox2D.CreateFromPoints(bounds.GetCorners()), Collision.LayerName.Enemy))
        {
            var shape = new CollisionShape2D(bounds);
            if (actor.Shape.Intersects(shape) && actor is Collider collider)
            {
                var affect = new Affect(
                    GameObject,
                    collider.GameObject,
                    AffectType.Damage,
                    new DamageArgs(1));
                AffectResolver.Affect(affect);
            }
        }
    }
    
    //TODO TEMPORARY
    public event Action<int> damaged;
    public void Damage(int value)
    {
        damaged?.Invoke(value);
    }
}