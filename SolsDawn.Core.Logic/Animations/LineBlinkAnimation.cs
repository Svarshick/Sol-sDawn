using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class LineBlinkAnimation (
    bool isOneShot,
    float duration,
    Vector2 start,
    Vector2 end,
    Color trueColor,
    Color blickColor,
    float thickness,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; private set; }

    private double _startTime = Time.TotalGameTime.TotalSeconds;

    public void Draw()
    {
        var elapsedTime = Time.TotalGameTime.TotalSeconds - _startTime;
        var isTimeExpired = elapsedTime > duration;
        var isHalfTimeExpired = elapsedTime * 2 > duration;

        Color color;

        if (isTimeExpired)
        {
            color = trueColor;
            if (isOneShot)
            {
                IsFinished = true;
            }
            else {
                _startTime = Time.TotalGameTime.TotalSeconds;
            }
        }
        else if (isHalfTimeExpired)
        {
            var t = MathHelper.Clamp((float)(elapsedTime * 2 / duration - 1), 0f, 1f);
            color = Color.Lerp(blickColor, trueColor, t);
        }
        else
        {
            var t = MathHelper.Clamp((float)(elapsedTime * 2 / duration), 0f, 1f);
            color = Color.Lerp(trueColor, blickColor, t);
        }
        
        Game.SpriteBatch.DrawLine(
            start,
            end,
            color,
            thickness,
            layerDepth);
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}