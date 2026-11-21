namespace SolsDawn.Gameplay;

[ProjectionModel]
public class DotProjection : IProjectionModel
{
    public float Calculate(Vector2 velocity, Vector2 tangent) => Vector2.Dot(velocity, tangent);
}

[AimAxisModel(AllowedAimAxis.Blade)]
public class AimAxis : IAimAxisModel
{
    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity, Vector2 aim, Vector2 tangent)
    {
        var axis = tangent == Vector2.Zero ? aim : CombatMath.TangentProjection(aim, tangent);
        return (axis, velocity.Length());
    }
}

[AimAxisModel(AllowedAimAxis.Fire)]
public class ReverseAimAxis : IAimAxisModel
{
    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity, Vector2 aim, Vector2 tangent)
    {
        var axis = tangent == Vector2.Zero ? -aim : CombatMath.TangentProjection(-aim, tangent);
        return (axis, velocity.Length());
    }
}

[BounceAxisModel]
public class FixedAxis : IBounceAxisModel
{
    [Inspect] public Vector2 Axis = new(0f, 1f);
    [Inspect(0.5f, 0f)] public float Speed = 15f;

    public (Vector2 Axis, float Speed) Calculate(Vector2 velocity)
    {
        var axis = Axis == Vector2.Zero ? Vector2.UnitY : Vector2.Normalize(Axis);
        return (axis, Speed);
    }
}
