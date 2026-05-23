using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossFireTelegraphAnimation : IAnimation
{
    public bool IsFinished { get; private set; }
    private RectangleBlinkAnimation _bossBlinkAnimation;
    private LineBlinkAnimation _fireBlinkAnimation;

    public BossFireTelegraphAnimation(BossStats stats, Vector2 position, Vector2 lookPosition)
    {
        _bossBlinkAnimation = new RectangleBlinkAnimation(
            true,
            stats.FireTelegraphDuration,
            new Transform() { Position = position },
            stats.Width,
            stats.Height,
            stats.Color,
            stats.FireTelegraphBlinkColor);

        var fireEnd = position + Vector2.Normalize(lookPosition - position) * stats.FireDistance;
        _fireBlinkAnimation = new LineBlinkAnimation(
            true,
            stats.FireTelegraphDuration,
            position,
            fireEnd,
            stats.FireTraceEndColor,
            stats.FireParryTraceStartColor,
            stats.FireWidth);
    }

    public void Draw()
    {
        _bossBlinkAnimation.Draw();
        _fireBlinkAnimation.Draw();
        IsFinished = _bossBlinkAnimation.IsFinished || _fireBlinkAnimation.IsFinished;
    }

    public void Cancel()
    {
        _bossBlinkAnimation.Cancel();
        _fireBlinkAnimation.Cancel();
    }
}