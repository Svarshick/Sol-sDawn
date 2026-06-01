using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class CircleTrace(
    Transform transform,
    float radius,
    int sides,
    float thickness,
    float duration,
    Color startColor,
    Color endColor,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; private set; }

    private readonly double _startTime = Time.TotalGameTime.TotalSeconds;

    public void Draw()
    {
        IsFinished = IsFinished || Time.TotalGameTime.TotalSeconds - _startTime > duration;
        if (IsFinished)
            return;
        
        var elapsedTime = Time.TotalGameTime.TotalSeconds - _startTime;
        var t = MathHelper.Clamp((float)(elapsedTime / duration), 0f, 1f);
        Game.SpriteBatch.DrawCircle(
            transform.Position,
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