using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossBladeTelegraphAnimation : IAnimation
{
    public bool IsFinished { get; private set; }
    private StarBlink _starBlink;
    private RectangleBlink _bossBlink;

    public BossBladeTelegraphAnimation(BossStats stats, Vector2 position, Vector2 lookPosition)
    {
        var starPosition = position + Vector2.Normalize(lookPosition - position) * stats.BladeTelegraphStarDistance;
        _starBlink = new StarBlink(
            new Transform { Position = starPosition },
            stats.BladeTelegraphStarStartAngle,
            stats.BladeTelegraphStarDeltaAngle,
            stats.BladeTelegraphStarInnerRadius,
            stats.BladeTelegraphStarOuterRadius,
            stats.BladeTelegraphStarThickness,
            stats.BladeTelegraphStarDuration,
            true,
            stats.BladeTelegraphStarColor,
            stats.BladeTelegraphBlinkColor);
        _bossBlink = new RectangleBlink(
            new Transform() { Position = position },
            stats.Width,
            stats.Height,
            stats.BladeTelegraphDuration,
            true,
            stats.Color,
            stats.BladeTelegraphBlinkColor);
    }

    public void Draw()
    {
        _bossBlink.Draw();
        _starBlink.Draw();
        IsFinished = _bossBlink.IsFinished || _starBlink.IsFinished;
    }

    public void Cancel()
    {
        _bossBlink.Cancel();
        _starBlink.Cancel();
    }
}