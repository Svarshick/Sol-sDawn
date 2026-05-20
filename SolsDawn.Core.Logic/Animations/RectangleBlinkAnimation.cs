using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class RectangleBlinkAnimation(
    bool isOneShot,
    SpriteBatch spriteBatch,
    float duration,
    Vector2 center,
    float width,
    float height,
    Color trueColor,
    Color blickColor,
    float layerDepth = 0.0f) 
    : IAnimation
{
    public bool IsFinished { get; private set; }
    
    private double _startTime = Time.TotalGameTime.TotalSeconds;
    public void Draw(GameTime gameTime)
    {
        var elapsedTime = gameTime.TotalGameTime.TotalSeconds - _startTime;
        var isTimeExpired = elapsedTime > duration;
        var isHalfTimeExpired = elapsedTime * 2 > duration;

        Color color;

        if (isTimeExpired)
        {
            color = trueColor;
            if (!isOneShot)
            {
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

        spriteBatch.FillRectangle(
            center.X - width / 2,
            center.Y - height / 2,
            width,
            height,
            color,
            layerDepth);
    }
    
    public void Cancel() => IsFinished = true;
}