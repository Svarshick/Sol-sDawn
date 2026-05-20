using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class RectangleIdleAnimation(
    SpriteBatch spriteBatch,
    Transform transform,
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
            transform.Position.X - width / 2,
            transform.Position.Y - height / 2,
            width,
            height,
            color,
            layerDepth);
    }

    public void Cancel()
    {
    }
}