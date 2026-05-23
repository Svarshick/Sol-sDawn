using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class StarBlinkAnimation(
    bool isOneShot,
    float duration,
    Transform transform,
    float startRotation,
    float deltaRotation,
    float maxInnerRadius,
    float maxOuterRadius,
    Color trueColor,
    Color blickColor,
    float thickness = 1.0f,
    float layerDepth = 0.0f) 
    : IAnimation
{
    public bool IsFinished { get; private set; }
    
    private double _startTime = Time.TotalGameTime.TotalSeconds;
    private readonly Vector2[] _vertices = new Vector2[9];

    public void Draw()
    {
        var elapsedTime = Time.TotalGameTime.TotalSeconds - _startTime;
        var isTimeExpired = elapsedTime > duration;
        var isHalfTimeExpired = elapsedTime * 2 > duration;

        Color color;
        float currentRotation;
        float currentSize;

        if (isTimeExpired)
        {
            color = trueColor;
            currentRotation = startRotation + deltaRotation;
            currentSize = 0f;
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
            currentRotation = startRotation + (float)(elapsedTime / duration) * deltaRotation;
            currentSize = MathHelper.Lerp(maxOuterRadius, 0f, t);
        }
        else
        {
            var t = MathHelper.Clamp((float)(elapsedTime * 2 / duration), 0f, 1f);
            color = Color.Lerp(trueColor, blickColor, t);
            currentRotation = startRotation + (float)(elapsedTime / duration) * deltaRotation;
            currentSize = MathHelper.Lerp(0f, maxOuterRadius, t);
        }

        float outerRadius = currentSize;
        float innerRadius = currentSize * (maxInnerRadius / maxOuterRadius);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathHelper.PiOver4 + currentRotation;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            
            _vertices[i] = new Vector2(
                (float)Math.Cos(angle) * r,
                (float)Math.Sin(angle) * r
            );
        }
        _vertices[8] = _vertices[0];

        Game.SpriteBatch.DrawPolygon(
            transform.Position,
            _vertices,
            color,
            thickness,
            layerDepth);
    }

    public void Cancel() => IsFinished = true;
}