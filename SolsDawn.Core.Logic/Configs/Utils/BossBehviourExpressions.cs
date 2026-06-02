using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Configs.Utils;


//VECTORS

public class PlayerPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Player.GameObject.Transform.Position;
}

public class PlayerPositionSnapshotExpression : VectorExpression
{
    private Vector2 _snapshot;
    private bool _isEvaluated;

    public override Vector2 Evaluate(FightBlackboard context)
    {
        if (!_isEvaluated)
        {
            _snapshot = context.Player.GameObject.Transform.Position;
            _isEvaluated = true;
        }

        return _snapshot;
    }
}

public class BossPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Boss.GameObject.Transform.Position;
}

public class CameraCenterPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Layout.CameraCenter();
}

public class CameraTopLeftPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Layout.CameraTopLeft();
}

public class CameraTopRightPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Layout.CameraTopRight();
}

public class CameraBottomLeftPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Layout.CameraBottomLeft();
}

public class CameraBottomRightPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(FightBlackboard context) => context.Layout.CameraBottomRight();
}


//FLOAT

public class UnitsFloatExpression(float units) : FloatExpression
{
    public override float Evaluate(FightBlackboard context) => context.Layout.ToPixels(units);
}


//BOOL

public class IsBossLastBladeSuccessExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsBossLastBladeSuccess;
}

public class IsBossLastBladeParriedExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsBossLastBladeParried;
}

public class IsBossLastFireSuccessExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsBossLastFireSuccess;
}

public class IsBossLastFireParriedExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsBossLastFireParried;
}

public class IsPlayerLastBladeSuccessExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsPlayerLastBladeSuccess;
}

public class IsPlayerLastFireSuccessExpression : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) => context.IsPlayerLastFireSuccess;
}