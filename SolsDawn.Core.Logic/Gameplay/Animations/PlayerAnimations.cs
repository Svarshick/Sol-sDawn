using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class PlayerAnimations() : AnimationPlayer(Idle)
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    private Animation _baseAnimation;
    private Animation _overlayAnimation;
    private readonly PlayerStats _stats = MainConfig.PlayerStats;

    public override void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdleAnimation(
                    _stats.Width,
                    _stats.Height,
                    _stats.Color);
                _baseAnimation.Transform.Parent = Transform;
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

    public override void Update()
    { 
        _baseAnimation?.Update();
    }

    public override void LateUpdate()
    {
        _overlayAnimation?.LateUpdate();
    }

    public override void Draw()
    {/*
        if (_overlayAnimation is { IsFinished: false })
        {
            _overlayAnimation.Draw();
        }
        else*/
        
        _baseAnimation?.Draw();
    }
}