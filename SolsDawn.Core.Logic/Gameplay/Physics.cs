using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Physics
{
    private static readonly List<Collider> Colliders = new();
    private static readonly List<Collider> CollidersToAdd = new();
    private static int _activeCasts;

    internal static void Add(Collider collider) => CollidersToAdd.Add(collider);

    private static void Sync()
    {
        if (_activeCasts > 0)
            return;

        int alive = 0;
        for (int i = 0; i < Colliders.Count; i++)
        {
            if (!Colliders[i].IsDestroyed)
                Colliders[alive++] = Colliders[i];
        }
        Colliders.RemoveRange(alive, Colliders.Count - alive);

        foreach (var collider in CollidersToAdd)
        {
            if (!collider.IsDestroyed)
                Colliders.Add(collider);
        }
        CollidersToAdd.Clear();
    }

    public static CircleCastResult CircleCast(
        Vector2 from,
        Vector2 to,
        float radius,
        Category layers,
        CircleCastFilter? filter = null)
    {
        var bestHit = new CircleCastResult { Hit = false, Fraction = 1f };

        AABB movementBounds;
        Vector2.Min(ref from, ref to, out movementBounds.LowerBound);
        Vector2.Max(ref from, ref to, out movementBounds.UpperBound);
        movementBounds.LowerBound -= new Vector2(radius, radius);
        movementBounds.UpperBound += new Vector2(radius, radius);

        var toiInput = new TOIInput
        {
            ProxyA = new DistanceProxy(new CircleShape(radius, 1f), 0),
            SweepA = new Sweep { C0 = from, C = to },
            TMax = 1
        };

        Sync();
        _activeCasts++;
        try
        {
            foreach (var collider in Colliders)
            {
                if (collider.IsDestroyed || (collider.Layer & layers) == Category.None)
                    continue;

                var pose = collider.Pose;
                var position = collider.Transform.WorldPosition;
                var rotation = collider.Transform.WorldRotation;
                toiInput.SweepB = new Sweep { C0 = position, C = position, A0 = rotation, A = rotation };

                var shape = collider.Shape;
                for (int childIndex = 0; childIndex < shape.ChildCount; childIndex++)
                {
                    shape.ComputeAABB(out var aabb, ref pose, childIndex);
                    if (!AABB.TestOverlap(ref movementBounds, ref aabb))
                        continue;

                    toiInput.ProxyB = new DistanceProxy(shape, childIndex);
                    TimeOfImpact.CalculateTimeOfImpact(out var toiOutput, ref toiInput);
                    if (toiOutput.State == TOIOutputState.Touching && toiOutput.T < bestHit.Fraction)
                    {
                        if (filter?.Invoke(from, to, radius, toiOutput.T, collider) == false)
                            continue;
                        bestHit.Hit = true;
                        bestHit.Fraction = toiOutput.T;
                        bestHit.Collider = collider;
                    }
                }
            }
        }
        finally
        {
            _activeCasts--;
        }

        return bestHit;
    }
}

public delegate bool CircleCastFilter(Vector2 from, Vector2 to, float radius, float fraction, Collider other);

public struct CircleCastResult
{
    public bool Hit;
    public float Fraction;
    public Collider Collider;
}

public sealed class Collider : Component
{
    public Shape Shape;
    public readonly Category Layer;

    private readonly ColliderAnimation _animation;
    private readonly double _activationTime;

    public Collider(GameObject go, Shape shape, Category layer, Debug.Collider.Category debugCategory = Debug.Collider.Category.Default) : base(go)
    {
        Shape = shape;
        Layer = layer;
        _activationTime = Time.TotalGameTime.TotalSeconds;
        _animation = new ColliderAnimation(debugCategory, this, 1);
        _animation.Transform.Position = Transform.WorldPosition;
        _animation.Transform.Rotation = Transform.WorldRotation;
        Game.AnimationsPool?.Add(_animation);
        Physics.Add(this);
    }

    public nkast.Aether.Physics2D.Common.Transform Pose => new(Transform.WorldPosition, Transform.WorldRotation);

    public override void Update()
    {
        _animation.Transform.Position = Transform.WorldPosition;
        _animation.Transform.Rotation = Transform.WorldRotation;
    }

    public override void OnDestroyImmediate()
    {
        float minimalTime = _animation.Category switch
        {
            Debug.Collider.Category.Default => Debug.Collider.DefaultMinimalTime,
            Debug.Collider.Category.Attack => Debug.Collider.AttackMinimalTime,
            Debug.Collider.Category.Parry => Debug.Collider.ParryMinimalTime,
            _ => throw Log.Physics.FatalException()
        };

        var timeToEnd = _activationTime + minimalTime - Time.TotalGameTime.TotalSeconds;
        if (timeToEnd > 0)
        {
            _animation.Transform.Position = Transform.WorldPosition;
            _animation.Transform.Rotation = Transform.WorldRotation;
            _animation.TimeToKill = timeToEnd;
        }
        else
        {
            _animation.Kill();
        }
    }
}

public class ColliderAnimation : Animation
{
    public readonly Debug.Collider.Category Category;
    public readonly float Layer;
    private readonly Collider _collider;

    public ColliderAnimation(Debug.Collider.Category category, Collider collider, float layer = 0)
    {
        Category = category;
        Layer = layer;
        _collider = collider;
    }

    public override void Draw()
    {
        Color color;
        float thickness;

        switch (Category)
        {
            case Debug.Collider.Category.Default:
                color = Debug.Collider.DefaultColor;
                thickness = Debug.Collider.DefaultThickness;
                break;
            case Debug.Collider.Category.Parry:
                color = Debug.Collider.ParryColor;
                thickness = Debug.Collider.ParryThickness;
                break;
            case Debug.Collider.Category.Attack:
                color = Debug.Collider.AttackColor;
                thickness = Debug.Collider.AttackThickness;
                break;
            default:
                throw Log.Physics.FatalException();
        }

        Game.Painter.BorderShape(Layer, _collider.Shape, Transform.WorldPosition, Transform.Rotation, color, thickness);
    }
}
