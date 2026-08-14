using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public class RectangleIdleAnimation(
    float width,
    float height,
    Color color,
    float layer = 0.0f)
    : Animation
{
    public override void Draw()
    {
        SolsDawn.Painter.FillRectangle(
            layer,
            Transform.WorldPosition,
            new (width, height),
            color);
    }
}