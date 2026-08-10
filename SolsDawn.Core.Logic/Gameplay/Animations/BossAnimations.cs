using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossAnimations() : AnimationPlayer(Idle)
{
    public const string Idle = "Idle";
    public const string BladeTelegraph = "Telegraph";
    public const string BladeParried = "BladeParried";
    public const string FireTelegraph = "FireTelegraph";
    public const string FireParried = "FireParried";
    public const string Hit = "Hit";

    public Vector2 LookPosition;
    
    private IAnimation _baseAnimation;
    private IAnimation _overlayAnimation;
    private readonly BossStats _stats = MainConfig.BossStats;

    public override void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation?.Cancel();
                _baseAnimation = new RectangleIdle(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color);
                break;
            case BladeTelegraph:
                _overlayAnimation?.Cancel();
                _overlayAnimation = null;
                _baseAnimation?.Cancel();
                _baseAnimation = new BossBladeTelegraphAnimation(_stats, Transform.Position, LookPosition);
                break;
            case BladeParried:
                _baseAnimation?.Cancel();
                _baseAnimation = new RectangleIdle(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.BladeParriedColor);
                break;
            case FireTelegraph:
                _overlayAnimation?.Cancel();
                _overlayAnimation = null;
                _baseAnimation = new BossFireTelegraphAnimation(_stats, Transform, LookPosition);
                break;
            case FireParried:
                _baseAnimation?.Cancel();
                _baseAnimation = new RectangleIdle(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.FireParriedColor);
                break;
            case Hit:
                _overlayAnimation?.Cancel();
                _overlayAnimation = new RectangleBlink(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.HitDuration,
                    true,
                    _stats.Color,
                    _stats.HitBlinkColor);
                break;
        }
    }

    public override void Draw()
    {
        if (_overlayAnimation is { IsFinished: false })
        {
            _overlayAnimation.Draw();
        }
        else
        {
            if (_baseAnimation is null)
                TryPlay(DefaultAnimation);
            
            if (_baseAnimation is BossFireTelegraphAnimation fireTelegraph)
            {
                fireTelegraph.LookPosition = LookPosition;
            }

            _baseAnimation.Draw();
        }
    }
}