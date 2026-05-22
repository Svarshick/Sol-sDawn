using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossAnimations : IAnimationPlayer
{
    public const string Idle = "Idle";
    public const string BladeTelegraph = "Telegraph";
    public const string BladeParried = "BladeParried";
    public const string FireTelegraph = "FireTelegraph";
    public const string FireParried = "FireParried";
    public const string Hit = "Hit";

    public Transform Transform { get; set; }
    
    private IAnimation _baseAnimation;
    private IAnimation _overlayAnimation;
    private readonly BossStats _stats = MainConfig.BossStats;

    public void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdleAnimation(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color);
                break;
            case BladeTelegraph:
                _overlayAnimation = null;
                _baseAnimation = new RectangleBlinkAnimation(
                    true,
                    _stats.BladeTelegraphDuration,
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color,
                    _stats.BladeTelegraphBlinkColor);
                break;
            case BladeParried:
                _baseAnimation = new RectangleIdleAnimation(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.BladeParriedColor);
                break;
            case FireTelegraph:
                _overlayAnimation = null;
                _baseAnimation = new RectangleBlinkAnimation(
                    true,
                    _stats.FireTelegraphDuration,
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color,
                    _stats.FireTelegraphBlinkColor);
                break;
            case FireParried:
                _baseAnimation = new RectangleIdleAnimation(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.FireParriedColor);
                break;
            case Hit:
                _overlayAnimation = new RectangleBlinkAnimation(
                    true,
                    _stats.HitDuration,
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color,
                    _stats.HitBlinkColor);
                break;
        }
    }

    public void Draw()
    {
        if (_overlayAnimation is { IsFinished: false })
        {
            _overlayAnimation.Draw();
        }
        else
        {
            _baseAnimation.Draw();
        }
    }
}