using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;

namespace SolsDawn.Core.Logic.Gameplay;

public sealed class HUD : Component<HUD>, IDrawable
{
    private readonly Player _player;
    private readonly SpriteBatch _spriteBatch;
    private readonly ScreenLayout _layout;
    
    public HUD(GameObject go, Player player, SpriteBatch spriteBatch, ScreenLayout layout) : base(go)
    {
        _player = player;
        _spriteBatch = spriteBatch;
        _layout = layout;
    }

    public override void Dispose()
    {
    }

    public void Draw(GameTime gameTime)
    {
        var mouseState = MouseExtended.GetState();
        var mousePosition = _layout.Camera.ScreenToWorld(mouseState.Position.ToVector2());
        var bladeDirection = mousePosition - _player.GameObject.Transform.Position;
        bladeDirection.Normalize();
        _spriteBatch.DrawCircle(
            _player.GameObject.Transform.Position + bladeDirection * _player.Stats.BladeAimDistance,
            _player.Stats.BladeAimRadius,
            20,
            _player.Stats.BladeAimColor,
            _player.Stats.BladeAimRadius);

        var indicatorRadius = _layout.ToPixels(0.5f);
        var indicatorPadding = _layout.ToPixels(0.3f);
        var indicatorY = _layout.CameraTopLeft().Y + indicatorRadius + indicatorPadding;
        var indicatorX = _layout.CameraTopLeft().X + indicatorRadius + indicatorPadding;
        
        if (_player.TeleportCharged)
        {
            _spriteBatch.DrawCircle(
                indicatorX,
                indicatorY,
                indicatorRadius,
                20,
                _player.Stats.TeleportEndColor,
                indicatorRadius,
                0.9f
            );
        }

        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.BladeCharged)
        {
            _spriteBatch.DrawCircle(
                indicatorX,
                indicatorY,
                indicatorRadius,
                20,
                _player.Stats.BladeTraceStartColor,
                indicatorRadius,
                0.9f
            );
        }
        
        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.FireCharged)
        {
            _spriteBatch.DrawCircle(
                indicatorX,
                indicatorY,
                indicatorRadius,
                20,
                _player.Stats.FireTraceStartColor,
                indicatorRadius,
                0.9f
            );
        }
    }
}