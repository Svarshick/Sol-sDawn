using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Entities;

public class EnemyAnimations(EnemyBoard board) : AnimationSet
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
        var color = _blinkRemaining > 0 ? specs.HitBlinkColor : board.Tint ?? specs.Color;
        Game.Painter.FillRectangle(0, Transform.WorldPosition, new Vector2(specs.Width, specs.Height), color);
    }
}
