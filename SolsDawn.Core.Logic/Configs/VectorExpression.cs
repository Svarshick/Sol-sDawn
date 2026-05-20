using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Configs;


public abstract class VectorExpression
{
    public abstract Vector2 Evaluate(BossBehaviourContext context);
    
    public static VectorExpression operator +(VectorExpression left, VectorExpression right) 
        => new AddVectorExpression(left, right);
    
    public static VectorExpression operator +(VectorExpression left, Vector2 right) 
        => new AddVectorExpression(left, new ConstantVectorExpression(right.X, right.Y));
    
    public static VectorExpression operator +(Vector2 left, VectorExpression right) 
        => new AddVectorExpression(new ConstantVectorExpression(left.X, left.X), right);
    
    public static VectorExpression operator -(VectorExpression left, VectorExpression right) 
        => new SubtractVectorExpression(left, right);

    public static VectorExpression operator -(Vector2 left, VectorExpression right)
        => new SubtractVectorExpression(new ConstantVectorExpression(left.X, left.Y), right);
    
    public static VectorExpression operator -(VectorExpression left, Vector2 right)
        => new SubtractVectorExpression(left, new ConstantVectorExpression(right.X, right.Y));
    
    public static VectorExpression operator *(VectorExpression left, float scalar) 
        => new ScaleVectorExpression(left, scalar);
    
    public static VectorExpression operator *(float scalar, VectorExpression right) 
        => new ScaleVectorExpression(right, scalar);
    
    public static VectorExpression operator /(VectorExpression left, float scalar)
        => new ScaleVectorExpression(left, 1/scalar);
}

public class ConstantVectorExpression(float x, float y) : VectorExpression
{
    private readonly Vector2 _value = new(x, y);
    public override Vector2 Evaluate(BossBehaviourContext context) => _value;
}

public class UnitsVectorExpression(float x, float y) : VectorExpression
{
    private readonly Vector2 _units = new(x, y);
    public override Vector2 Evaluate(BossBehaviourContext context) => context.Layout.ToPixels(_units);
}

public class PlayerPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) => context.Player.GameObject.Position;
}

public class BossPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) => context.Boss.GameObject.Position;
}

public class CameraCenterPositionExpression : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) => context.Layout.CameraCenter();
}

public class AddVectorExpression(VectorExpression left, VectorExpression right) : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) 
        => left.Evaluate(context) + right.Evaluate(context);
}

public class SubtractVectorExpression(VectorExpression left, VectorExpression right) : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) 
        => left.Evaluate(context) - right.Evaluate(context);
}

public class ScaleVectorExpression(VectorExpression operand, float scalar) : VectorExpression
{
    public override Vector2 Evaluate(BossBehaviourContext context) 
        => operand.Evaluate(context) * scalar;
}