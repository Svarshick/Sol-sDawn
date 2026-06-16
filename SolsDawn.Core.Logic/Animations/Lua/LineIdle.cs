using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations.Lua;

public class LineIdle(
    Vector2 point1,
    Vector2 point2,
    float thickness,
    Color color,
    float layerDepth = 0.0f)
    : IAnimation
{
    public Vector2 Point1 = point1;
    public Vector2 Point2 = point2;
    public Color Color = color;
    public float Thickness = thickness;
    public float LayerDepth = layerDepth;

    public bool IsFinished { get; private set; } = false;

    public void Draw()
    {
        Game.SpriteBatch.DrawLine(
            Point1,
            Point2,
            Color,
            Thickness,
            LayerDepth
        );
    }

    public void Cancel() => IsFinished = true;
}