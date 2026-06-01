using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossFireTelegraphAnimation : IAnimation
{
    public bool IsFinished { get; private set; }
    private RectangleBlink _bossBlink;
    private LineBlink _fireBlink;

    public BossFireTelegraphAnimation(BossStats stats, Vector2 position, Vector2 lookPosition)
    {
        _bossBlink = new RectangleBlink(
            new Transform() { Position = position },
            stats.Width,
            stats.Height,
            stats.FireTelegraphDuration,
            true,
            stats.Color,
            stats.FireTelegraphBlinkColor);

        var fireEnd = position + Vector2.Normalize(lookPosition - position) * stats.FireDistance;
        _fireBlink = new LineBlink(
            new Transform { Position = position },
            fireEnd,
            stats.FireWidth,
            true,
            stats.FireTelegraphDuration,
            stats.FireTraceEndColor,
            stats.FireParryTraceStartColor);
    }

    public void Draw()
    {
        _bossBlink.Draw();
        _fireBlink.Draw();
        IsFinished = _bossBlink.IsFinished || _fireBlink.IsFinished;
    }

    public void Cancel()
    {
        _bossBlink.Cancel();
        _fireBlink.Cancel();
    }
}