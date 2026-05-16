using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Effects;

public class LineTrace : IEffect
{
    public readonly Vector2 Start;
    public readonly Vector2 End;
    public readonly Color Color;
    public readonly float Thickness;

    private SpriteBatch _spriteBatch;
    private double _duration;
    private double _startTime;

    public LineTrace(
        SpriteBatch spriteBatch,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness,
        float duration)
    {
        _spriteBatch = spriteBatch;

        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;

        _duration = duration;
        _startTime = Time.TotalGameTime.TotalSeconds;
    }

    public bool IsFinished { get; private set; }

    public void Update(GameTime gameTime)
    {
        IsFinished = gameTime.TotalGameTime.TotalSeconds - _startTime > _duration;
    }

    public void LateUpdate(GameTime gameTime)
    {
    }

    public void Draw(GameTime gameTime)
    {
        _spriteBatch.DrawLine(
            Start,
            End,
            Color,
            Thickness
        );
    }
}