using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class CircleBlickAnimation(
    bool isOneShot,
    SpriteBatch spriteBatch,
    float duration,
    Transform transform,
    float radius,
    int sides,
    Color trueColor,
    Color blickColor,
    float thickness,
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

        spriteBatch.DrawCircle(
            transform.Position,
            radius,
            sides,
            color,
            thickness,
            layerDepth);
    }
    
    public void Cancel() => IsFinished = true;
}