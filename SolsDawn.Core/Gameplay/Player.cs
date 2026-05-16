using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using SolsDawn.Core.Configs;
using SolsDawn.Core.Effects;

namespace SolsDawn.Core.Gameplay;

public record PlayerStats(
    float Velocity,
    float BladeDistance,
    float TeleportMinDistance,
    float TeleportMaxDistance,
    float TeleportHoldDuration,
    float TeleportThickness,
    Color TeleportStartColor,
    Color TeleportEndColor,
    float TeleportTraceThickness,
    Color TeleportTraceStartColor,
    Color TeleportTraceEndColor
);

public class Player : IUpdatable, IDrawable
{
    public readonly PlayerStats Stats;
    
    public Vector2 Position { get; private set; } = Vector2.Zero;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
    private bool _moveDirectionUpdated;
    private bool _moveLock;
    
    private Line _teleportLine;
    private SpriteBatch _spriteBatch;
    private EffectsPool _effectsPool;
    private ScreenLayout _layout;
    public Player(
        SpriteBatch spriteBatch, 
        EffectsPool effectsPool,
        ScreenLayout layout, 
        Input input)
    {
        _spriteBatch = spriteBatch;
        _effectsPool = effectsPool;
        _layout = layout;

        var defaultStats = MainConfig.PlayerStats;
        Stats = new(
            _layout.ToPixels(defaultStats.Velocity),
           _layout.ToPixels(defaultStats.BladeDistance),
            _layout.ToPixels(defaultStats.TeleportMinDistance),
            _layout.ToPixels(defaultStats.TeleportMaxDistance),
            defaultStats.TeleportHoldDuration,
            _layout.ToPixels(defaultStats.TeleportThickness),
            defaultStats.TeleportStartColor,
            defaultStats.TeleportEndColor,
            _layout.ToPixels(defaultStats.TeleportTraceThickness),
            defaultStats.TeleportTraceStartColor,
            defaultStats.TeleportTraceEndColor
        );
            
        input.Move += Move;
        input.TeleportStarted += TeleportStarted;
        input.TeleportUpdated += TeleportUpdated;
        input.TeleportReleased += TeleportReleased;
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
    
    public void Update(GameTime gameTime)
    {
        if (_moveDirectionUpdated)
        {
            var velocity = Stats.Velocity;
            var shift = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += shift * MoveDirection;
        }
    }

    public void LateUpdate(GameTime gameTime)
    {
        _moveDirectionUpdated = false;
    }
    
    public void Draw(GameTime gameTime)
    {
        var radius = _layout.ToPixels(0.5f);
        _spriteBatch.DrawCircle(
            center: Position,
            radius: radius,
            sides: 20,
            color: Color.Blue,
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
}