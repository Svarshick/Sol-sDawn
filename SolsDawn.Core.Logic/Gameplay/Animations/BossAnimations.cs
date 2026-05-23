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
                _baseAnimation = new RectangleIdleAnimation(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color);
                break;
            case BladeTelegraph:
                _overlayAnimation = null;
                var starPosition = Transform.Position + Vector2.Normalize(LookPosition - Transform.Position) * _stats.BladeTelegraphStarDistance;
                var starBlinkAnimation = new StarBlinkAnimation(
                    true,
                    _stats.BladeTelegraphStarDuration,
                    new Transform() { Position = starPosition },
                    _stats.BladeTelegraphStarStartAngle,
                    _stats.BladeTelegraphStarDeltaAngle,
                    _stats.BladeTelegraphStarInnerRadius,
                    _stats.BladeTelegraphStarOuterRadius,
                    _stats.BladeTelegraphStarColor,
                    _stats.BladeTelegraphBlinkColor,
                    _stats.BladeTelegraphStarThickness);
                var bossBlinkAnimation = new RectangleBlinkAnimation(
                    true,
                    _stats.BladeTelegraphDuration,
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color,
                    _stats.BladeTelegraphBlinkColor);
                _baseAnimation = new BossTelegraphAnimation(bossBlinkAnimation, starBlinkAnimation);
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
            _baseAnimation.Draw();
        }
    }
}