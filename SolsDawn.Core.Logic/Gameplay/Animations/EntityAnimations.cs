using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class EntityAnimations : AnimationPlayer
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    private IAnimation _baseAnimation;
    private IAnimation _overlayAnimation;
    private EntityStats _stats;

    public EntityAnimations(EntityStats stats) : base(Idle)
    {
        _stats = stats;
    }
    
    public override void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdle(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.Color);
                break;
            case Hit:/*
                _overlayAnimation = new RectangleBlink(
                    Transform,
                    _stats.Width,
                    _stats.Height,
                    _stats.HitInvulnerabilityDuration,
                    true,
                    _stats.Color,
                    _stats.HitBlinkColor);*/
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