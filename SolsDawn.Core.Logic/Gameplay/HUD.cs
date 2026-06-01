using MonoGame.Extended;
using MonoGame.Extended.Input;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay;

public sealed class HUD : Component<HUD>, IDrawable
{
    private readonly Player _player;
    
    public HUD(GameObject go, Player player) : base(go)
    {
        _player = player;
    }

    public override void Dispose()
    {
    }

    public void Draw()
    {
        var mouseState = MouseExtended.GetState();
        var mousePosition = Game.ScreenLayout.Camera.ScreenToWorld(mouseState.Position.ToVector2());
        Game.SpriteBatch.DrawCircle(
            mousePosition,
            _player.Stats.CursorRadius,
            20,
            _player.Stats.CursorColor,
            _player.Stats.CursorRadius);

        var indicatorRadius = Game.ScreenLayout.ToPixels(0.5f);
        var indicatorPadding = Game.ScreenLayout.ToPixels(0.3f);
        var indicatorY = Game.ScreenLayout.CameraTopLeft().Y + indicatorRadius + indicatorPadding;
        var indicatorX = Game.ScreenLayout.CameraTopLeft().X + indicatorRadius + indicatorPadding;
        
        if (_player.TeleportCharged)
        {
            Game.SpriteBatch.DrawCircle(
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
            Game.SpriteBatch.DrawCircle(
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
            Game.SpriteBatch.DrawCircle(
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