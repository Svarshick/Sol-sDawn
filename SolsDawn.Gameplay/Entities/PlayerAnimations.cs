namespace SolsDawn.Gameplay.Entities;

public class PlayerAnimations(PlayerBoard board) : AnimationSet
{
    private Animation _baseAnimation;
    private Animation _overlayAnimation;
    private (float Width, float Height, Color Color) _appliedLook;

    public override void TryPlay(string animationName)
    {
        var specs = board.Specs.GetActiveInstance();
        switch (animationName)
        {
            case Idle:
                _appliedLook = (specs.Width, specs.Height, specs.Color);
                _baseAnimation = new RectangleIdleAnimation(
                    specs.Width,
                    specs.Height,
                    specs.Color);
                _baseAnimation.Transform.Parent = Transform;
                break;
            case Hit:
                break;
        }
    }

    public override void Update()
    {
        var specs = board.Specs.GetActiveInstance();
        if (_baseAnimation != null && (specs.Width, specs.Height, specs.Color) != _appliedLook)
            TryPlay(Idle);

        _baseAnimation?.Update();
    }

    public override void LateUpdate() => _baseAnimation?.LateUpdate();
    public override void Draw() => _baseAnimation?.Draw();
}