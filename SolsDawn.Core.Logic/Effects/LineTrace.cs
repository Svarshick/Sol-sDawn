using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Effects;

public class LineTrace : IEffect
{
    private Vector2 _start;
    private Vector2 _end;
    private Color _startColor;
    private Color _endColor;
    private float _thickness;

    private SpriteBatch _spriteBatch;
    private double _duration;
    private double _startTime;

    public LineTrace(
        SpriteBatch spriteBatch,
        Vector2 start,
        Vector2 end,
        Color startColor,
        Color endColor,
        float thickness,
        float duration)
    {
        _spriteBatch = spriteBatch;

        _start = start;
        _end = end;
        _startColor = startColor;
        _endColor = endColor;
        _thickness = thickness;

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
        var elapsedTime = gameTime.TotalGameTime.TotalSeconds - _startTime;
        var t = MathHelper.Clamp((float)(elapsedTime / _duration), 0f, 1f);
        _spriteBatch.DrawLine(
            _start,
            _end,
            Color.Lerp(_startColor, _endColor, t),
            _thickness
        );
    }
}