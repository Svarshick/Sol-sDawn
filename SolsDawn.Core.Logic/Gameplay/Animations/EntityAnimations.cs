using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class EntityAnimations : AnimationPlayer
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    private Animation _baseAnimation;
    private Animation _overlayAnimation;
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
        _baseAnimation?.LateUpdate();
    }

    public override void Draw()
    {
        _baseAnimation?.Draw();
    }
}