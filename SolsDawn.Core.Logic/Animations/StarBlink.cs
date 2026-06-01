using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class StarBlink(
    Transform transform,
    float startAngle,
    float deltaAngle,
    float maxInnerRadius,
    float maxOuterRadius,
    float thickness,
    float duration,
    bool isOneShot,
    Color trueColor,
    Color blickColor,
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
        float currentAngle;
        float currentSize;

        if (isTimeExpired)
        {
            color = trueColor;
            currentAngle = startAngle + deltaAngle;
            currentSize = 0f;
            if (isOneShot)
            {
                IsFinished = true;
            }
            else
            {
                _startTime = Time.TotalGameTime.TotalSeconds;
            }
        }
        else if (isHalfTimeExpired)
        {
            var t = MathHelper.Clamp((float)(elapsedTime * 2 / duration - 1), 0f, 1f);
            color = Color.Lerp(blickColor, trueColor, t);
            currentAngle = startAngle + (float)(elapsedTime / duration) * deltaAngle;
            currentSize = MathHelper.Lerp(maxOuterRadius, 0f, t);
        }
        else
        {
            var t = MathHelper.Clamp((float)(elapsedTime * 2 / duration), 0f, 1f);
            color = Color.Lerp(trueColor, blickColor, t);
            currentAngle = startAngle + (float)(elapsedTime / duration) * deltaAngle;
            currentSize = MathHelper.Lerp(0f, maxOuterRadius, t);
        }

        float outerRadius = currentSize;
        float innerRadius = currentSize * (maxInnerRadius / maxOuterRadius);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathHelper.PiOver4 + currentAngle;
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