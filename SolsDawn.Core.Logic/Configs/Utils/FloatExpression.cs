namespace SolsDawn.Core.Logic.Configs.Utils;

public abstract class FloatExpression
{
    public abstract float Evaluate(BossBehaviourContext context);

    public static BoolExpression operator >(FloatExpression left, FloatExpression right)
        => new GreaterThanBoolExpression(left, right);

    public static BoolExpression operator <(FloatExpression left, FloatExpression right)
        => new LessThanBoolExpression(left, right);

    public static BoolExpression operator >=(FloatExpression left, FloatExpression right)
        => new GreaterThanOrEqualBoolExpression(left, right);

    public static BoolExpression operator <=(FloatExpression left, FloatExpression right)
        => new LessThanOrEqualBoolExpression(left, right);

    public static BoolExpression operator ==(FloatExpression left, FloatExpression right)
        => new EqualBoolExpression(left, right);

    public static BoolExpression operator !=(FloatExpression left, FloatExpression right)
        => new NotBoolExpression(new EqualBoolExpression(left, right));

    public static implicit operator FloatExpression(float value)
        => new ConstantFloatExpression(value);

    public static FloatExpression operator +(FloatExpression left, FloatExpression right)
        => new AddFloatExpression(left, right);

    public static FloatExpression operator -(FloatExpression left, FloatExpression right)
        => new SubtractFloatExpression(left, right);

    public static FloatExpression operator *(FloatExpression left, FloatExpression right)
        => new MultiplyFloatExpression(left, right);

    public static FloatExpression operator /(FloatExpression left, FloatExpression right)
        => new DivideFloatExpression(left, right);
}

public class ConstantFloatExpression(float value) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) => value;
}

public class UnitsFloatExpression(float units) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) => context.Layout.ToPixels(units);
}

public class AddFloatExpression(FloatExpression left, FloatExpression right) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) 
        => left.Evaluate(context) + right.Evaluate(context);
}

public class SubtractFloatExpression(FloatExpression left, FloatExpression right) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) 
        => left.Evaluate(context) - right.Evaluate(context);
}

public class MultiplyFloatExpression(FloatExpression left, FloatExpression right) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) 
        => left.Evaluate(context) * right.Evaluate(context);
}

public class DivideFloatExpression(FloatExpression left, FloatExpression right) : FloatExpression
{
    public override float Evaluate(BossBehaviourContext context) 
    {
        float denom = right.Evaluate(context);
        return denom == 0 ? 0 : left.Evaluate(context) / denom;
    }
}