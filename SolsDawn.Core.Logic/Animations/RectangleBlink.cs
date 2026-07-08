using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class RectangleBlink(
    Transform2 transform,
    float width,
    float height,
    float duration,
    bool isOneShot,
    Color trueColor,
    Color blickColor,
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

        Game.SpriteBatch.FillRectangle(
            transform.Position.X - width / 2,
            transform.Position.Y - height / 2,
            width,
            height,
            color,
            layerDepth);
    }
    
    public void Cancel() => IsFinished = true;
}