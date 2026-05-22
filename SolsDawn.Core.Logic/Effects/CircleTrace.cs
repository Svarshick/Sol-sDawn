using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Effects;

public class CircleTrace(
    float duration,
    Vector2 center,
    float radius,
    int sides,
    Color startColor,
    Color endColor,
    float thickness,
    float layerDepth = 0.0f)
    : IEffect
{
    public bool IsFinished { get; private set; }

    private readonly double _startTime = Time.TotalGameTime.TotalSeconds;

    public void Draw(GameTime gameTime)
    {
        IsFinished = IsFinished || gameTime.TotalGameTime.TotalSeconds - _startTime > duration;
        if (IsFinished)
            return;
        
        var elapsedTime = gameTime.TotalGameTime.TotalSeconds - _startTime;
        var t = MathHelper.Clamp((float)(elapsedTime / duration), 0f, 1f);
        Game.SpriteBatch.DrawCircle(
            center,
            radius,
            sides,
            Color.Lerp(startColor, endColor, t),
            thickness,
            layerDepth);
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}