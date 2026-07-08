using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class LineBlink (
    Transform2 transform,
    Vector2 end,
    float thickness,
    bool isOneShot,
    float duration,
    Color trueColor,
    Color blickColor,
    float layerDepth = 0.0f)
    : IAnimation
{
    public bool IsFinished { get; private set; }
    public Vector2 End = end;

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
            transform.Position,
            End,
            color,
            thickness,
            layerDepth);
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}