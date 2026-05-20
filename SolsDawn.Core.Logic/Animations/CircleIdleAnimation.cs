using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class CircleIdleAnimation(
    SpriteBatch spriteBatch,
    Vector2 position,
    float radius,
    int sides,
    Color color,
    float thickness,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; } = true;

    public void Draw(GameTime gameTime)
    {
        spriteBatch.DrawCircle(
            position,
            radius,
            sides,
            color,
            thickness,
            layerDepth);
    }

    public void Cancel()
    {
    }
}