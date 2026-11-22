using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal class RectangleIdleAnimation(
    float width,
    float height,
    Color color,
    float layer = 0.0f)
    : Animation
{
    public override void Draw()
    {
        Game.Painter.FillRectangle(
            layer,
            Transform.WorldPosition,
            new (width, height),
            color);
    }
}