using System;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal interface ISpeedSamples
{
    int FrameRate { get; }
    long First { get; }
    long Last { get; }
    bool TryGet(long frame, out Vector2 velocity);
}

internal static class SpeedSamplesExtensions
{
    public static bool Contains(this ISpeedSamples samples, long frame) =>
        frame >= samples.First && frame < samples.Last;
}

internal sealed class SpeedRecording : ISpeedSamples
{
    public const int FramesPerSecond = 60;
    public const int LimitSeconds = 300;
    public const int Capacity = FramesPerSecond * LimitSeconds;

    private readonly Vector2[] _values = new Vector2[Capacity];
    private readonly bool[] _present = new bool[Capacity];

    public int FrameRate => FramesPerSecond;
    public long First => Math.Max(0, Last - Capacity);
    public long Last { get; private set; }
    public bool IsStopped { get; set; }

    public void Add(Vector2? velocity)
    {
        if (IsStopped)
            return;

        var index = (int)(Last % Capacity);
        _present[index] = velocity.HasValue;
        _values[index] = velocity ?? default;
        Last++;
    }

    public void Clear() => Last = 0;

    public bool TryGet(long frame, out Vector2 velocity)
    {
        velocity = default;
        if (!this.Contains(frame))
            return false;

        var index = (int)(frame % Capacity);
        velocity = _values[index];
        return _present[index];
    }
}

internal sealed class SavedGraph : ISpeedSamples
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int FrameRate { get; set; } = SpeedRecording.FramesPerSecond;
    public long CreatedTicks { get; set; } = DateTime.UtcNow.Ticks;
    public float?[] X { get; set; } = [];
    public float?[] Y { get; set; } = [];

    [JsonIgnore] public long First => 0;
    [JsonIgnore] public long Last => X.Length;
    [JsonIgnore] public double DurationSeconds => (double)X.Length / FrameRate;

    public bool TryGet(long frame, out Vector2 velocity)
    {
        velocity = default;
        if (!this.Contains(frame) || X[frame] is not { } x || Y[frame] is not { } y)
            return false;

        velocity = new Vector2(x, y);
        return true;
    }

    public static bool TryClip(ISpeedSamples source, long start, long end, out long from, out long to)
    {
        from = Math.Max(start, source.First);
        to = Math.Min(end, source.Last);
        return from < to;
    }

    public void SetSamples(ISpeedSamples source, long from, long to)
    {
        var length = (int)(to - from);
        var xs = new float?[length];
        var ys = new float?[length];
        for (var i = 0; i < length; i++)
        {
            if (source.TryGet(from + i, out var velocity))
            {
                xs[i] = velocity.X;
                ys[i] = velocity.Y;
            }
        }

        X = xs;
        Y = ys;
        FrameRate = source.FrameRate;
    }
}
