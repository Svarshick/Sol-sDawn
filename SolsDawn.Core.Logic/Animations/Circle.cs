
using Microsoft.Xna.Framework;
namespace SolsDawn.Core.Logic.Animations;

public class CircleIdleAnimation
    : Animation
{
    public float Radius;
    public Color Color;
    public float Layer;

    public CircleIdleAnimation(
        float radius,
        Color color,
        float layer = 0.0f)
    {
        Radius = radius;
        Color = color;
        Layer = layer;
    }
    
    public override void Draw()
    {
        Game.Painter.FillCircle(
            Layer,
            Transform.WorldPosition,
            Radius,
            Color);
    }
}