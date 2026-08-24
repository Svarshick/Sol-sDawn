namespace SolsDawn.Gameplay.Entities;

public class DefaultAnimation() : AnimationPlayer(Idle)
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    private Animation _baseAnimation;
    private Animation _overlayAnimation;
    private readonly PlayerBoard _board = Main.PlayerBoard;

    public override void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdleAnimation(
                    _board.Config.Width,
                    _board.Config.Height,
                    _board.Config.Color);
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