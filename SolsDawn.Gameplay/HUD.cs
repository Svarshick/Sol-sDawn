using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision;

namespace SolsDawn.Gameplay;

public sealed class HUD : Component
{
    private readonly Player _player;
    
    public HUD(GameObject go, Player player) : base(go)
    {
        _player = player;
    }

    public override void Draw()
    {
        var indicatorRadius = 0.5f;
        var indicatorPadding = 0.3f;
        var indicatorY = Camera.TopLeft.Y - indicatorRadius - indicatorPadding;
        var indicatorX = Camera.TopLeft.X + indicatorRadius + indicatorPadding;
        
        if (_player.Board.TeleportCharged)
        {
            Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Board.Config.TeleportEndColor,
                indicatorRadius);
        }

        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.Board.BladeCharged)
        {
            Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Board.Config.BladeTraceColor);
        }
        
        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.Board.FireCharged)
        {
            Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Board.Config.FireTraceColor);
        }

        if (Camera.Contains(Vector2.Zero) == ContainmentType.Disjoint)
        {
            var bounds = Camera.BoundingBox;
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
                Painter.FillCircle(
                    1,
                    hitPoint,
                    0.5f,
                    Color.White);
            }
        }
    }
}