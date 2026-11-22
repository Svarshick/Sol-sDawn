
using Microsoft.Xna.Framework;
namespace SolsDawn.Engine;

public class CircleIdleAnimation
    : Animation
{
    public float Radius;
    public Color Color;
    public float Layer;

    internal CircleIdleAnimation(
        float radius,
        Color color,
        float layer = 0.0f)
    {
        Radius = radius;
        Color = color;
        Layer = layer;
    }
    
    public override void Draw()
    {
        Game.Painter.FillCircle(
            Layer,
            Transform.WorldPosition,
            Radius,
            Color);
    }
}

internal class CircleTraceAnimation : Animation
{
    private readonly float _radius;
    private readonly double _duration;
    private readonly Color _startColor;
    private readonly float _layer;
    private double _elapsedTime;
    private Color _lerpedColor;

    public CircleTraceAnimation(
        Vector2 center,
        float radius,
        double duration,
        Color startColor,
        float layer = 0.0f)
    {
        Transform.Position = center;
        _radius = radius;
        _duration = duration;
        _startColor = startColor;
        _lerpedColor = startColor;
        _layer = layer;
    }

    protected override void OnReset()
    {
        _elapsedTime = 0;
    }

    public override void Update()
    {
        _elapsedTime += Time.ElapsedGameTime.TotalSeconds;
        if (_elapsedTime >= _duration)
        {
            State = AnimationState.Finished;
            _lerpedColor = Color.Transparent;
        }
        else
        {
            var t = MathHelper.Clamp((float)(_elapsedTime / _duration), 0f, 1f);
            _lerpedColor = Color.Lerp(_startColor, Color.Transparent, t);
        }
    }

    public override void Draw()
    {
        Game.Painter.FillCircle(
            _layer,
            Transform.WorldPosition,
            _radius,
            _lerpedColor);
    }
}
