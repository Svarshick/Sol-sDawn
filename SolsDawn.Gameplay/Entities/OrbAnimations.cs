namespace SolsDawn.Gameplay.Entities;

public class OrbAnimations(OrbBoard board) : AnimationPlayer(Idle)
{
    public const string Idle = "Idle";
    private Animation _animation;

    public override void TryPlay(string animationName)
    {
        _animation = new CircleIdleAnimation(board.Specs.Radius, board.Specs.Color);
        _animation.Transform.Parent = Transform;
    }

    public override void Update() => _animation?.Update();
    public override void LateUpdate() => _animation?.LateUpdate();
    public override void Draw() => _animation?.Draw();
}