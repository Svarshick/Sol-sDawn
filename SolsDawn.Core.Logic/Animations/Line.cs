using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public class LineIdleAnimation
    : Animation
{
    public Vector2 End;
    public float Thickness;
    public Color Color;
    public float Layer;
    
    public LineIdleAnimation(
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        float layer = 0.0f)
    {
        Transform.Position = start;
        End = end;
        Thickness = thickness;
        Color = color;
        Layer = layer;
    }
    
    public override void Draw()
    {
        SolsDawn.Painter.FillLine(
            Layer,
            Transform.WorldPosition,
            End,
            Thickness,
            Color);
    }
}

public class LineTraceAnimation : Animation
{
    public Vector2 End;
    
    private float _thickness;
    private double _duration;
    private double _elapsedTime;
    private Color _startColor;
    private Color _lerpedColor;
    private float _layer;

    public LineTraceAnimation(
        Vector2 start,
        Vector2 end,
        float thickness,
        double duration,
        Color startColor,
        float layer = 0.0f)
    {
        Transform.Position = start;
        End = end;
        _thickness = thickness;
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
        SolsDawn.Painter.FillLine(
            _layer,
            Transform.Position,
            End,
            0,
            _lerpedColor,
            _thickness);
    }
}