using System;

namespace SolsDawn.Core.Logic.Animations;

public class DelegatedAnimation(
    Action drawer,
    float duration)
    : IAnimation
{
    public bool IsFinished { get; private set; }
    private readonly double _startTime = Time.TotalGameTime.TotalSeconds;

    public void Draw()
    {
        IsFinished = IsFinished || Time.TotalGameTime.TotalSeconds - _startTime > duration;
        if (IsFinished)
            return;

        drawer();
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}