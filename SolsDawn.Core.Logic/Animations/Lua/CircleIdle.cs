using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations.Lua;

public class CircleIdle(
    Vector2 position,
    float radius,
    int sides,
    float thickness,
    Color color,
    float layerDepth = 0.0f)
    : IAnimation
{
    public Vector2 Position = position;
    public float Radius = radius;
    public Color Color = color;
    
    public bool IsFinished { get; private set; } = false;

    public void Draw()
    {
        if (IsFinished)
            return;
        Game.SpriteBatch.DrawCircle(
            Position,
            Radius,
            sides,
            Color,
            thickness,
            layerDepth);
    }

    public void Cancel() => IsFinished = true;
}