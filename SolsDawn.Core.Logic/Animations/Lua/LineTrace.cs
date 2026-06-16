using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations.Lua;

public class LineTrace(
    Vector2 point1,
    Vector2 point2,
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
        Game.SpriteBatch.DrawLine(
            point1,
            point2,
            Color.Lerp(startColor, endColor, t),
            thickness,
            layerDepth);
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}