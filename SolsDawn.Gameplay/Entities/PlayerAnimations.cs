namespace SolsDawn.Gameplay.Entities;

public class PlayerAnimations(PlayerBoard board) : AnimationPlayer
{
    private Animation _baseAnimation;
    private Animation _overlayAnimation;

    public override void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdleAnimation(
                    board.Specs.Width,
                    board.Specs.Height,
                    board.Specs.Color);
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
    {/*
        if (_overlayAnimation is { IsFinished: false })
        {
            _overlayAnimation.Draw();
        }
        else*/
        
        _baseAnimation?.Draw();
    }
}