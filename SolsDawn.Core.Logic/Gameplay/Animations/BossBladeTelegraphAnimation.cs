using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossBladeTelegraphAnimation : IAnimation
{
    public bool IsFinished { get; private set; }
    private StarBlinkAnimation _starBlinkAnimation;
    private RectangleBlinkAnimation _bossBlinkAnimation;

    public BossBladeTelegraphAnimation(BossStats stats, Vector2 position, Vector2 lookPosition)
    {
        var starPosition = position + Vector2.Normalize(lookPosition - position) * stats.BladeTelegraphStarDistance;
        _starBlinkAnimation = new StarBlinkAnimation(
            true,
            stats.BladeTelegraphStarDuration,
            new Transform() { Position = starPosition },
            stats.BladeTelegraphStarStartAngle,
            stats.BladeTelegraphStarDeltaAngle,
            stats.BladeTelegraphStarInnerRadius,
            stats.BladeTelegraphStarOuterRadius,
            stats.BladeTelegraphStarColor,
            stats.BladeTelegraphBlinkColor,
            stats.BladeTelegraphStarThickness);
        _bossBlinkAnimation = new RectangleBlinkAnimation(
            true,
            stats.BladeTelegraphDuration,
            new Transform() { Position = position },
            stats.Width,
            stats.Height,
            stats.Color,
            stats.BladeTelegraphBlinkColor);
    }

    public void Draw()
    {
        _bossBlinkAnimation.Draw();
        _starBlinkAnimation.Draw();
        IsFinished = _bossBlinkAnimation.IsFinished || _starBlinkAnimation.IsFinished;
    }

    public void Cancel()
    {
        _bossBlinkAnimation.Cancel();
        _starBlinkAnimation.Cancel();
    }
}