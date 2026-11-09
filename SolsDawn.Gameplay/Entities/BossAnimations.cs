namespace SolsDawn.Gameplay.Entities;

public class BossAnimations(BossBoard board) : AnimationSet
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
            case Hit:
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
    {
        _baseAnimation?.Draw();
    }
}