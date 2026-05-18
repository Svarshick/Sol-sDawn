using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Effects;

namespace SolsDawn.Core.Logic.Gameplay;

public class PlayerStats
{
    public Color Color;
    [Units] public float Size;
    [Units] public float Velocity;
    [Units] public float BladeDistance;
    [Units] public float TeleportMinDistance;
    [Units] public float TeleportMaxDistance;
    public float TeleportHoldDuration;
    [Units] public float TeleportThickness;
    public Color TeleportStartColor;
    public Color TeleportEndColor;
    [Units] public float TeleportTraceThickness;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public sealed class Player : Component<Player>, IUpdatable, IDrawable
{
    public readonly PlayerStats Stats;
    
    public Vector2 Position { get; private set; } = Vector2.Zero;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
    private bool _moveDirectionUpdated;
    private bool _moveLock;
    
    private Line _teleportLine;
    private readonly Collider _collider;
    private readonly SpriteBatch _spriteBatch;
    private readonly EffectsPool _effectsPool;
    private readonly ScreenLayout _layout;
    public Player(
        GameObject go,
        SpriteBatch spriteBatch, 
        EffectsPool effectsPool,
        ScreenLayout layout, 
        Input input) : base(go)
    {
        _collider = GameObject.GetComponent<Collider>() ?? throw new NullReferenceException("Can't get collider component");
        _spriteBatch = spriteBatch;
        _effectsPool = effectsPool;
        _layout = layout;

        Stats = ConfigReader.Read(MainConfig.PlayerStats, _layout);
            
        input.Move += Move;
        input.TeleportStarted += TeleportStarted;
        input.TeleportUpdated += TeleportUpdated;
        input.TeleportReleased += TeleportReleased;
    }

    public override void Dispose() { }

    public void Update(GameTime gameTime)
    {
        if (_moveDirectionUpdated)
        {
            var velocity = Stats.Velocity;
            var shift = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += shift * MoveDirection;
        }
        
        var bounds = new BoundingCircle2D(Position, Stats.Size/2);
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate(GameTime gameTime)
    {
        _moveDirectionUpdated = false;
    }
    
    public void Draw(GameTime gameTime)
    {
        var radius = Stats.Size/2;
        _spriteBatch.DrawCircle(
            center: Position,
            radius: radius,
            sides: 20,
            color: Stats.Color,
            thickness: radius 
        );

        var mouseState = MouseExtended.GetState();
        var mousePosition = _layout.Camera.ScreenToWorld(mouseState.Position.ToVector2());
        var bladeDirection = mousePosition - Position;
        bladeDirection.Normalize();
        var bladeRadius = _layout.ToPixels(0.2f);
        _spriteBatch.DrawCircle(
            center: Position + bladeDirection * Stats.BladeDistance,
            radius: bladeRadius,
            sides: 20,
            color: Color.Aqua,
            thickness: bladeRadius
        );
    }
    
    private void TeleportStarted(Vector2 screenPosition)
    {
        _moveLock = true;
        var mousePosition = _layout.Camera.ScreenToWorld(screenPosition);
        var endPosition = TeleportPosition(mousePosition, 0);
        _teleportLine = new(_spriteBatch, Position, endPosition, Stats.TeleportStartColor, Stats.TeleportThickness);
        _effectsPool.Add(_teleportLine);
    }

    private void TeleportUpdated(Vector2 screenPosition, double elapsedTime)
    {
        _teleportLine.Start = Position;
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
            Position,
            endPosition,
            Stats.TeleportTraceStartColor,
            Stats.TeleportTraceEndColor,
            Stats.TeleportTraceThickness,
            2
            ));
        
        Position = endPosition;
        _moveLock = false;
    }
    
    private float TeleportLerp(double elapsedTime)
    {
        return MathHelper.Clamp((float)(elapsedTime / Stats.TeleportHoldDuration), 0f, 1f);
    }
    
    private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
    {
        var teleportDirection = pointPosition - Position;
        teleportDirection.Normalize();
        var delta = lerp * (Stats.TeleportMaxDistance - Stats.TeleportMinDistance);
        return Position + teleportDirection * (Stats.TeleportMinDistance + delta);
    }
    
    private void Move(Vector2 moveDirection)
    {
        if (_moveLock)
            return;
        MoveDirection = moveDirection;
        _moveDirectionUpdated = true;
    }
}