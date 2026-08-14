using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public class ArrowTraceAnimation : Animation
{
    public Vector2 Direction;
    public float TailLength;
    public float TailWidth;
    public float HeadLength;
    public float HeadWidth;
    public Color StartColor;
    public float Layer;
    
    private Color _lerpedColor;
    private double _duration;
    private double _elapsedTime;
    
    public ArrowTraceAnimation(
        double duration,
        Vector2 from,
        Vector2 direction,
        float tailLength,
        float tailWidth,
        float headLength,
        float headWidth,
        Color startColor,
        float layer = 0)
    {
        _duration = duration;
        Transform.Position = from;
        Direction = direction;
        TailLength = tailLength;
        TailWidth = tailWidth;
        HeadLength = headLength;
        HeadWidth = headWidth;
        StartColor = startColor;
        Layer = layer;
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
            _lerpedColor = Color.Lerp(StartColor, Color.Transparent, t);
        }
    }

    public override void Draw()
    {
        SolsDawn.Painter.FillArrow(
            Layer,
            Transform.WorldPosition,
            Direction,
            TailLength,
            TailWidth,
            HeadLength,
            HeadWidth,
            _lerpedColor);
    }
}

public class ArrowPentagonTraceAnimation : Animation
{
    public Vector2 Direction;
    public float TailLength;
    public float TailWidth;
    public float HeadLength;
    public float HeadWidth;
    public Color StartColor;
    public float Layer;
    
    private Color _lerpedColor;
    private double _duration;
    private double _elapsedTime;
    
    public ArrowPentagonTraceAnimation(
        double duration,
        Vector2 from,
        Vector2 direction,
        float tailLength,
        float tailWidth,
        float headLength,
        float headWidth,
        Color startColor,
        float layer = 0)
    {
        _duration = duration;
        Transform.Position = from;
        Direction = direction;
        TailLength = tailLength;
        TailWidth = tailWidth;
        HeadLength = headLength;
        HeadWidth = headWidth;
        StartColor = startColor;
        Layer = layer;
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
            _lerpedColor = Color.Lerp(StartColor, Color.Transparent, t);
        }
    }

    public override void Draw()
    {
        SolsDawn.Painter.FillArrowPentagon(
            Layer,
            Transform.WorldPosition,
            Direction,
            TailLength,
            TailWidth,
            HeadLength,
            HeadWidth,
            _lerpedColor);
    }
}