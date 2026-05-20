using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class RectangleIdleAnimation(
    SpriteBatch spriteBatch,
    Vector2 center,
    float width,
    float height,
    Color color,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; } = true;

    public void Draw(GameTime time)
    {
        spriteBatch.FillRectangle(
            center.X - width / 2,
            center.Y - height / 2,
            width,
            height,
            color,
            layerDepth);
    }

    public void Cancel()
    {
    }
}