using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class CircleIdle(
    Transform transform,
    float radius,
    int sides,
    float thickness,
    Color color,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; set; } = false;

    public void Draw()
    {
        Game.SpriteBatch.DrawCircle(
            transform.Position,
            radius,
            sides,
            color,
            thickness,
            layerDepth);
    }

    public void Cancel() => IsFinished = true;
}