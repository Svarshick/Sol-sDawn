using System;

namespace SolsDawn.Core.Logic.Configs.Utils;

public abstract class BoolExpression
{
    public abstract bool Evaluate(FightBlackboard context);

    public static BoolExpression operator &(BoolExpression left, BoolExpression right)
        => new AndBoolExpression(left, right);

    public static BoolExpression operator |(BoolExpression left, BoolExpression right)
        => new OrBoolExpression(left, right);

    public static BoolExpression operator !(BoolExpression operand)
        => new NotBoolExpression(operand);

    // There is no && and || overloading. Instead
    // x && y = T.false(x) ? x : T.&(x, y)
    // x || y = T.true(x) ? x : T.|(x, y)
    public static bool operator true(BoolExpression expr) => false;
    public static bool operator false(BoolExpression expr) => false;

    public static implicit operator FightActionCondition(BoolExpression expr)
        => ctx => expr.Evaluate(ctx);
}

public class AndBoolExpression(BoolExpression left, BoolExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) && right.Evaluate(context);
}

public class OrBoolExpression(BoolExpression left, BoolExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) || right.Evaluate(context);
}

public class NotBoolExpression(BoolExpression operand) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => !operand.Evaluate(context);
}

public class GreaterThanBoolExpression(FloatExpression left, FloatExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) > right.Evaluate(context);
}

public class LessThanBoolExpression(FloatExpression left, FloatExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) < right.Evaluate(context);
}

public class GreaterThanOrEqualBoolExpression(FloatExpression left, FloatExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) >= right.Evaluate(context);
}

public class LessThanOrEqualBoolExpression(FloatExpression left, FloatExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => left.Evaluate(context) <= right.Evaluate(context);
}

public class EqualBoolExpression(FloatExpression left, FloatExpression right) : BoolExpression
{
    public override bool Evaluate(FightBlackboard context) 
        => Math.Abs(left.Evaluate(context) - right.Evaluate(context)) < float.Epsilon;
}