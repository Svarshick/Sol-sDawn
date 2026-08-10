using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using nkast.Aether.Physics2D.Collision;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay;

public sealed class HUD : Component
{
    private readonly Player _player;
    
    public HUD(GameObject go, Player player) : base(go)
    {
        _player = player;
    }

    public override void Draw()
    {
        var mouseState = MouseExtended.GetState();
        var mousePosition = Game.Camera.ScreenToWorld(mouseState.Position.ToVector2());
        Game.SpriteBatch.DrawCircle(
            mousePosition,
            _player.Stats.CursorRadius,
            20,
            _player.Stats.CursorColor,
            _player.Stats.CursorRadius);

        var indicatorRadius = 0.5f;
        var indicatorPadding = 0.3f;
        var indicatorY = Game.Camera.TopLeft.Y - indicatorRadius - indicatorPadding;
        var indicatorX = Game.Camera.TopLeft.X + indicatorRadius + indicatorPadding;
        
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

        if (Game.Camera.Contains(Vector2.Zero) == ContainmentType.Disjoint)
        {
            var bounds = Game.Camera.BoundingBox;
            var start = Vector2.Zero;
            var end = _player.GameObject.Transform.Position;
            var input = new RayCastInput
            {
                Point1 = start,
                Point2 = end,
                MaxFraction = 1.0f
            };
            
            if (bounds.RayCast(out var output, ref input))
            {
                var hitPoint = start + output.Fraction * (end - start);
                Game.SpriteBatch.DrawCircle(
                    hitPoint,
                    0.5f,
                    20,
                    Color.White,
                    1,
                    0.95f);
            }
        }
    }
}