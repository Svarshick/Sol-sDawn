using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class PlayerAnimations : IAnimationPlayer
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    public Transform Transform { get; set; }

    private IAnimation _baseAnimation;
    private IAnimation? _overlayAnimation;
    private readonly PlayerStats _stats = MainConfig.PlayerStats;

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