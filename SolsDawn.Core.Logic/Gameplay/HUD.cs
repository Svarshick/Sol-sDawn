using Microsoft.Xna.Framework;
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
        var indicatorRadius = 0.5f;
        var indicatorPadding = 0.3f;
        var indicatorY = SolsDawn.Camera.TopLeft.Y - indicatorRadius - indicatorPadding;
        var indicatorX = SolsDawn.Camera.TopLeft.X + indicatorRadius + indicatorPadding;
        
        if (_player.TeleportCharged)
        {
            SolsDawn.Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Stats.TeleportEndColor,
                indicatorRadius);
        }

        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.BladeCharged)
        {
            SolsDawn.Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Stats.BladeTraceStartColor);
        }
        
        indicatorX += (indicatorRadius * 2 + indicatorPadding);
        if (_player.FireCharged)
        {
            SolsDawn.Painter.FillCircle(
                1,
                new Vector2(indicatorX, indicatorY),
                indicatorRadius,
                _player.Stats.FireTraceStartColor);
        }

        if (SolsDawn.Camera.Contains(Vector2.Zero) == ContainmentType.Disjoint)
        {
            var bounds = SolsDawn.Camera.BoundingBox;
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
                SolsDawn.Painter.FillCircle(
                    1,
                    hitPoint,
                    0.5f,
                    Color.White);
            }
        }
    }
}