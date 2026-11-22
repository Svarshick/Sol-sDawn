using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal enum ValueMode
{
    Total,
    Horizontal,
    Vertical
}

internal sealed class GraphView(long period)
{
    public const long MinPeriod = 2;
    public const double ZoomFactor = 1.25;

    private long _first;
    private long _last;
    private long _maxPeriod = long.MaxValue;

    public long Period { get; private set; } = period;
    public long End { get; private set; }
    public bool Following { get; private set; } = true;
    public long Start => End - Period;

    public void Sync(ISpeedSamples samples, long maxPeriod)
    {
        _first = samples.First;
        _last = samples.Last;
        _maxPeriod = Math.Max(MinPeriod, maxPeriod);
        if (Following)
            End = _last;
        Apply(End);
    }

    public void ShowAll(ISpeedSamples samples, long maxPeriod)
    {
        Period = samples.Last - samples.First;
        Following = true;
        Sync(samples, maxPeriod);
    }

    public void SetPeriod(long period)
    {
        Period = period;
        Apply(End);
    }

    public void MoveTo(long end) => Apply(end);

    public void ResizeStart(long start, long fixedEnd)
    {
        Period = fixedEnd - start;
        Apply(fixedEnd);
    }

    public void ResizeEnd(long fixedStart, long end)
    {
        Period = Math.Min(end, _last) - fixedStart;
        ClampPeriod();
        Apply(fixedStart + Period);
    }

    public void Zoom(double notches, double anchorFrame)
    {
        if (notches == 0)
            return;

        var oldPeriod = Period;
        var newPeriod = (long)Math.Round(oldPeriod * Math.Pow(ZoomFactor, -notches));
        if (newPeriod == oldPeriod)
            newPeriod += Math.Sign(-notches);

        if (Following)
        {
            Period = newPeriod;
            Apply(End);
            return;
        }

        var fraction = (anchorFrame - Start) / oldPeriod;
        Period = newPeriod;
        ClampPeriod();
        var newStart = anchorFrame - fraction * Period;
        Apply((long)Math.Round(newStart) + Period);
    }

    private void ClampPeriod() => Period = Math.Clamp(Period, MinPeriod, _maxPeriod);

    private void Apply(long end)
    {
        ClampPeriod();
        End = Math.Clamp(end, Math.Min(_first + Period, _last), _last);
        Following = End == _last;
    }
}

internal static class GraphMath
{
    public const float MinFrameSpacing = 6f;
    public const float MinMajorSpacing = 60f;
    public const float MinSpeedSpacing = 24f;

    public static long[] StepList(int frameRate) =>
    [
        1, 2, 3, 5, 10, 15, 30,
        frameRate, 2L * frameRate, 5L * frameRate, 10L * frameRate, 15L * frameRate, 30L * frameRate,
        60L * frameRate, 120L * frameRate, 300L * frameRate
    ];

    public static long FrameStep(long period, float width, int frameRate)
    {
        var steps = StepList(frameRate);
        foreach (var step in steps)
        {
            if (step * width / period >= MinFrameSpacing)
                return step;
        }

        return steps[^1];
    }

    public static long MajorStep(long frameStep, long period, float width, int frameRate)
    {
        var steps = StepList(frameRate);
        foreach (var step in steps)
        {
            if (step % frameStep == 0 && step * width / period >= MinMajorSpacing)
                return step;
        }

        return steps[^1];
    }

    public static float SpeedStep(float range, float height)
    {
        var min = MinSpeedSpacing * range / height;
        var step = MathF.Pow(10f, MathF.Floor(MathF.Log10(min)));
        foreach (var factor in new[] { 1f, 2f, 5f, 10f })
        {
            if (step * factor >= min * 0.9999f)
                return step * factor;
        }

        return step * 10f;
    }

    public static bool IsSigned(ValueMode mode, bool absolute) => mode != ValueMode.Total && !absolute;

    public static float Shown(Vector2 velocity, ValueMode mode, bool absolute) => mode switch
    {
        ValueMode.Total => velocity.Length(),
        ValueMode.Horizontal => absolute ? MathF.Abs(velocity.X) : velocity.X,
        _ => absolute ? MathF.Abs(velocity.Y) : velocity.Y
    };

    public static float AxisMax(ISpeedSamples samples, long start, long end, ValueMode mode, bool absolute, float field)
    {
        var max = Math.Max(0f, field);
        var from = Math.Max(start, samples.First);
        var to = Math.Min(end, samples.Last);
        for (var frame = from; frame < to; frame++)
        {
            if (samples.TryGet(frame, out var velocity))
            {
                var value = MathF.Abs(Shown(velocity, mode, absolute));
                if (value > max)
                    max = value;
            }
        }

        return max > 0f && float.IsFinite(max) ? max : 1f;
    }
}
