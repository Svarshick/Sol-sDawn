using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class PolygonTrace(
    Transform2 transform,
    IReadOnlyList<Vector2> vertices,
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
        Game.SpriteBatch.DrawPolygon(vertices,
            transform.Position,
            transform.Rotation,
            Color.Lerp(startColor, endColor, t),
            thickness,
            layerDepth
        );
    }
    
    public void Cancel()
    {
        IsFinished = true;
    }
}