using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class PlayerAnimations() : AnimationPlayer(Idle)
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    private IAnimation _baseAnimation;
    private IAnimation _overlayAnimation;
    private readonly PlayerStats _stats = MainConfig.PlayerStats;

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
            case Hit:
                _overlayAnimation = new RectangleBlinkAnimation(
                    true,
                    _stats.HitInvulnerabilityDuration,
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