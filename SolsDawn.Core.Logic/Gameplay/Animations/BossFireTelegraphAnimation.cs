using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossFireTelegraphAnimation : IAnimation
{
    public bool IsFinished { get; private set; }
    public Vector2 LookPosition; 
    
    private readonly RectangleBlink _bossBlink;
    private readonly LineBlink _fireBlink;
    private readonly BossStats _bossStats;
    private readonly Transform2 _startTransform; 

    public BossFireTelegraphAnimation(BossStats stats, Transform2 bossTransform, Vector2 lookPosition)
    {
        _bossStats = stats;
        _startTransform = bossTransform;
        _bossBlink = new RectangleBlink(
            bossTransform,
            stats.Width,
            stats.Height,
            stats.FireTelegraphDuration,
            true,
            stats.Color,
            stats.FireTelegraphBlinkColor);

        var fireEnd = bossTransform.Position + Vector2.Normalize(lookPosition - bossTransform.Position) * stats.FireDistance;
        _fireBlink = new LineBlink(
            bossTransform,
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
        _fireBlink.End = _startTransform.Position + Vector2.Normalize(LookPosition - _startTransform.Position) * _bossStats.FireDistance; 
        _fireBlink.Draw();
        IsFinished = _bossBlink.IsFinished || _fireBlink.IsFinished;
    }

    public void Cancel()
    {
        _bossBlink.Cancel();
        _fireBlink.Cancel();
    }
}