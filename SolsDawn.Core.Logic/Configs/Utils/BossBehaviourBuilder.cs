using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic.Configs.Utils;

public record BossBehaviourContext(Boss Boss, Player Player, ScreenLayout Layout);

public class BossBehaviourBuilder
{
    private readonly List<Action<BossBehaviourContext>> _actions = new();
    
    public BossBehaviourBuilder Wait(float seconds)
    {
        _actions.Add(ctx => ctx.Boss.Wait(seconds));
        return this;
    }

    public BossBehaviourBuilder Teleport(VectorExpression position)
    {
        _actions.Add(ctx => ctx.Boss.Teleport(position.Evaluate(ctx)));
        return this;
    }

    public BossBehaviourBuilder Blade(VectorExpression lookPosition)
    {
        _actions.Add(ctx => ctx.Boss.Blade(lookPosition.Evaluate(ctx)));
        return this;
    }
    
    public IReadOnlyList<Action<BossBehaviourContext>> Build() => _actions;

    public static VectorExpression Units(float x, float y) => new UnitsVectorExpression(x, y);
    public static VectorExpression Player => new PlayerPositionExpression();
    public static VectorExpression Boss => new BossPositionExpression();
    public static VectorExpression CameraCenter => new CameraCenterPositionExpression();
}