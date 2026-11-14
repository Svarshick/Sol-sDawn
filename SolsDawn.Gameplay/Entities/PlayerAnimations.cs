namespace SolsDawn.Gameplay.Entities;

public class PlayerAnimations(PlayerBoard board) : AnimationSet
{
    private float _blinkRemaining;

    public override void TryPlay(string animationName)
    {
        if (animationName == Hit)
            _blinkRemaining = board.Specs.GetActiveInstance().HitDuration;
    }

    public override void Update()
    {
        if (_blinkRemaining > 0)
            _blinkRemaining -= DeltaTime;
    }

    public override void Draw()
    {
        var specs = board.Specs.GetActiveInstance();
        var color = _blinkRemaining > 0 ? specs.HitBlinkColor : specs.Color;
        Game.Painter.FillCircle(0, Transform.WorldPosition, specs.ColliderRadius, color);
    }
}
