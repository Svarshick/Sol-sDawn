namespace SolsDawn.Gameplay;

[ProjectionModel]
public class DotProjection : IProjectionModel
{
    public float Calculate(Vector2 velocity, Vector2 tangent) => Vector2.Dot(velocity, tangent);
}

[Default]
[ProjectionModel]
public class LengthProjection : IProjectionModel
{
    public float Calculate(Vector2 velocity, Vector2 tangent) =>
        Sign(Vector2.Dot(velocity, tangent)) * velocity.Length();
}

[AimAxisModel(AllowedAimAxis.Blade)]
public class AimAxis : IAimAxisModel
{
    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity, Vector2 aim, Vector2 tangent)
    {
        var axis = tangent == Vector2.Zero ? aim : (Vector2.Dot(aim, tangent) >= 0 ? tangent : -tangent);
        return (axis, velocity.Length());
    }
}

[AimAxisModel(AllowedAimAxis.Fire)]
public class ReverseAimAxis : IAimAxisModel
{
    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity, Vector2 aim, Vector2 tangent)
    {
        var axis = tangent == Vector2.Zero ? -aim : (Vector2.Dot(aim, tangent) <= 0 ? tangent : -tangent);
        return (axis, velocity.Length());
    }
}

[AxisModel]
public class ReverseAxis : IAxisModel
{
    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity)
    {
        var axis = velocity == Vector2.Zero ? Vector2.UnitY : -Vector2.Normalize(velocity);
        return (axis, 0f);
    }
}
