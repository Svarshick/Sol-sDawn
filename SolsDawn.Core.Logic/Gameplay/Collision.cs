using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Collision
{
    public static Category Default => Category.Cat1;
    public static Category Player => Category.Cat2;
    public static Category Enemy => Category.Cat3;
    public static Category BladeAttack => Category.Cat4;
    public static Category FireAttack => Category.Cat5;
    public static Category BladeParry => Category.Cat6;
    public static Category FireParry => Category.Cat7;

    public static readonly World World;

    static Collision()
    {
        World = new World(Vector2.Zero);
    }
    
    public static void Update(GameTime gameTime)
    {
        foreach (var body in World.BodyList)
        {
            if (body.Tag is not Collider collider)
                continue;
            body.Position = collider.GameObject.Transform.Position;
            body.Rotation = collider.GameObject.Transform.Rotation;
        }

        var dt = gameTime.ElapsedGameTime;
        World.Step(dt);

        foreach (var body in World.BodyList)
        {
            if (body.Tag is not Collider collider)
                continue;
            collider.GameObject.Transform.Position = body.Position;
            collider.GameObject.Transform.Rotation = body.Rotation;
        }
    }
    
    public struct HitResult
    {
        public Fixture Fixture;
        public Vector2 HitPoint;
        public float Distance;
    }
    
    public static void Overlap(AABB aabb, Category layer, IList<GameObject> results)
    {
        World.QueryAABB(fixture =>
        {
            if ((fixture.CollisionCategories & layer) != 0)
            {
                if (fixture.Body.Tag is Collider collider)
                {
                    if (!results.Contains(collider.GameObject))
                    {
                        results.Add(collider.GameObject);
                    }
                }
            }

            return true;
        }, ref aabb);
    }
    
    public static void Overlap(
        Shape shape,
        Vector2 position,
        float rotation,
        Category layer,
        IList<GameObject> results)
    {
        var queryTransform = new nkast.Aether.Physics2D.Common.Transform(position, rotation);
        shape.ComputeAABB(out var aabb, ref queryTransform, 0);

        World.QueryAABB(fixture =>
        {
            if ((fixture.CollisionCategories & layer) != 0)
            {
                fixture.Body.GetTransform(out var bodyTransform);

                if (nkast.Aether.Physics2D.Collision.Collision.TestOverlap(
                        shape, 
                        0, 
                        fixture.Shape, 
                        0, 
                        ref queryTransform, 
                        ref bodyTransform))
                {
                    if (fixture.Body.Tag is Collider collider)
                    {
                        if (!results.Contains(collider.GameObject))
                        {
                            results.Add(collider.GameObject);
                        }
                    }
                }
            }

            return true;
        }, ref aabb);
    }
    
    public static bool LineCast(
        Vector2 start, 
        Vector2 end, 
        float width, 
        Category layer,
        out HitResult hit)
    {
        hit = default;

        var dir = end - start;
        var length = dir.Length();
        if (length <= 0.0001f) 
            return false;
        
        dir /= length;
        var normal = new Vector2(-dir.Y, dir.X) * (width * 0.5f);
        var vertices = new Vertices(4)
        {
            start + normal,
            end + normal,
            end - normal,
            start - normal
        };

        var beamShape = new PolygonShape(vertices, 0);
        var beamTransform = Transform.Identity;

        beamShape.ComputeAABB(out AABB aabb, ref beamTransform, 0);

        var beamProxy = new DistanceProxy(beamShape, 0);
        var minDistanceSq = float.MaxValue;
        Fixture? closestFixture = null;
        var closestPoint = Vector2.Zero;

        World.QueryAABB(fixture =>
        {
            if ((fixture.CollisionCategories & layer) != 0)
            {
                fixture.Body.GetTransform(out var fixtureTransform);

                if (nkast.Aether.Physics2D.Collision.Collision.TestOverlap(
                        beamShape,
                        0,
                        fixture.Shape,
                        0,
                        ref beamTransform,
                        ref fixtureTransform))
                {
                    var distInput = new DistanceInput
                    {
                        ProxyA = beamProxy,
                        ProxyB = new DistanceProxy(fixture.Shape, 0),
                        TransformA = beamTransform,
                        TransformB = fixtureTransform,
                        UseRadii = true
                    };

                    Distance.ComputeDistance(out DistanceOutput distOutput, out _, distInput);
                    var distSq = Vector2.DistanceSquared(start, distOutput.PointB);
                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
                        closestFixture = fixture;
                        closestPoint = distOutput.PointB;
                    }
                }
            }

            return true;
        }, ref aabb);

        if (closestFixture != null)
        {
            hit.Fixture = closestFixture;
            hit.HitPoint = closestPoint;
            hit.Distance = MathF.Sqrt(minDistanceSq);
            return true;
        }

        return false;
    }
}

public sealed class Collider : Component
{
    public bool Enabled
    {
        get => _body.Enabled;
        set
        {
            if (value && !_body.Enabled)
            {
                _activationTime = Time.TotalGameTime.TotalSeconds;
            }

            _body.Enabled = value;
            Animation.IsVisible = value;
        }
    }

    public event OnCollisionEventHandler OnCollision
    {
        add => _body.OnCollision += value;
        remove => _body.OnCollision -= value;
    }
    
    private ColliderAnimation Animation
    {
        get;
        set
        {
            field?.Kill();
            value.Transform.Position = _body.Position;
            value.Transform.Rotation = _body.Rotation;
            value.IsVisible = _body.Enabled;
            Game.AnimationsPool.Add(value);
            field = value;
        }
    }

    private double _activationTime;
    private Body _body;

    public Collider(
        GameObject go,
        Shape shape,
        Category selfLayer,
        Category collidesLayer,
        BodyType bodyType = BodyType.Dynamic,
        bool isSensor = false) : base(go)
    {
        _body = new Body
        {
            Position = go.Transform.Position, 
            Rotation = go.Transform.Rotation, 
            BodyType = bodyType,
            Tag = this
        };
        Fixture fixture = _body.CreateFixture(shape);
        fixture.Tag = this;
        fixture.CollisionCategories = selfLayer;
        fixture.CollidesWith = collidesLayer;
        fixture.IsSensor = isSensor;
        Collision.World.Add(_body);
        _activationTime = Time.TotalGameTime.TotalSeconds;
        
        Animation = new ColliderAnimation(Debug.Collider.Category.Default, _body.FixtureList, 1);
    }

    public override void Update()
    {
        Animation.Transform.Position = _body.Position;
        Animation.Transform.Rotation = _body.Rotation;
        Animation.IsVisible = _body.Enabled;
    }

    public override void OnDestroyImmediate()
    {
        float minimalTime = Animation.Category switch
        {
            Debug.Collider.Category.Default => Debug.Collider.DefaultMinimalTime,
            Debug.Collider.Category.Attack => Debug.Collider.AttackMinimalTime,
            Debug.Collider.Category.Parry => Debug.Collider.ParryMinimalTime,
            _ => throw new LogicException()
        };

        var timeToEnd = _activationTime + minimalTime - Time.TotalGameTime.TotalSeconds;
        if (timeToEnd > 0)
        {
            Animation.Transform.Position = _body.Position;
            Animation.Transform.Rotation = _body.Rotation;
            Animation.TimeToKill = timeToEnd;
        }
        else
        {
            Animation.Kill();
        }

        Collision.World.Remove(_body);
    }
}

public class ColliderAnimation : Animation
{
    public Debug.Collider.Category Category;
    public float Layer;
    public FixtureCollection FixtureList;

    public ColliderAnimation(
        Debug.Collider.Category category,
        FixtureCollection fixtureList,
        float layer = 0)
    {
        Category = category;
        Layer = layer;
        FixtureList = fixtureList;
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
                color =  Debug.Collider.ParryColor;
                thickness = Debug.Collider.ParryThickness;
                break;
            case Debug.Collider.Category.Attack:
                color = Debug.Collider.AttackColor;
                thickness = Debug.Collider.AttackThickness;
                break;
            default:
                throw new LogicException();
        }
        
        foreach (var fixture in FixtureList)
        {
            Game.Painter.BorderShape(Layer, fixture.Shape, Transform.WorldPosition, Transform.Rotation, color, thickness);
        }
    }
}