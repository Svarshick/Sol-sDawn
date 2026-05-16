using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SolsDawn.Core.Effects;

namespace SolsDawn.Core.Gameplay;

public class Player : IUpdatable, IDrawable
{
    public float UnitsVelocity { get; set; } = 5;
        
    public Vector2 Position { get; private set; } = Vector2.Zero;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
    private bool _moveDirectionUpdated;

    private Line _teleportLine;
    private double _teleportHoldDuration = 2;
    private float _teleportLineThickness = 2;
    private Color _teleportLineColor = Color.Black;

    private SpriteBatch _spriteBatch;
    private EffectsPool _effectsPool;
    private ScreenLayout _screenLayout;
    public Player(
        SpriteBatch spriteBatch, 
        EffectsPool effectsPool,
        ScreenLayout screenLayout, 
        Input input)
    {
        _spriteBatch = spriteBatch;
        _effectsPool = effectsPool;
        _screenLayout = screenLayout;
        input.Move += Move;
        input.TeleportStarted += TeleportStarted;
        input.TeleportUpdated += TeleportUpdated;
        input.TeleportReleased += TeleportReleased;
    }

    private void TeleportStarted(Vector2 position)
    {
        var endPosition = _screenLayout.Camera.ScreenToWorld(position);
        _teleportLine = new(_spriteBatch, Position, endPosition, Color.White, _teleportLineThickness);
        _effectsPool.Add(_teleportLine);
    }

    private void TeleportUpdated(Vector2 position, double elapsedTime)
    {
        _teleportLine.Start = Position;
        _teleportLine.End = _screenLayout.Camera.ScreenToWorld(position);
        var t = MathHelper.Clamp((float)(elapsedTime / _teleportHoldDuration), 0f, 1f);
        _teleportLine.Color = Color.Lerp(Color.White, _teleportLineColor, t);
    }

    private void TeleportReleased(Vector2 position, double elapsedTime)
    {
        _teleportLine.IsFinished = true;
        _teleportLine = null;
        
        var endPosition = _screenLayout.Camera.ScreenToWorld(position);
        
        _effectsPool.Add(new LineTrace(
            _spriteBatch,
            Position,
            endPosition,
            _teleportLineColor,
            _teleportLineThickness,
            2
            ));
        
        Position = endPosition;
    }
    
    private void Move(Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
        _moveDirectionUpdated = true;
    }
    
    public void Update(GameTime gameTime)
    {
        if (_moveDirectionUpdated)
        {
            var shift = _screenLayout.PixelsPerUnit * UnitsVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += shift * MoveDirection;
        }
    }

    public void LateUpdate(GameTime gameTime)
    {
        _moveDirectionUpdated = false;
    }
    
    public void Draw(GameTime gameTime)
    {
        var radius = _screenLayout.ToPixels(0.5f);
        _spriteBatch.DrawCircle(
            center: Position,
            radius: radius,
            sides: 20,
            color: Color.Blue,
            thickness: radius 
        );
    }
}