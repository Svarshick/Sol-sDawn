using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Collision
{
    public static Category Default => Category.Cat1;
    public static Category Player => Category.Cat2;
    public static Category Enemy => Category.Cat3;
    public static Category Parry => Category.Cat4;

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
        }
    }

    public event OnCollisionEventHandler OnCollision
    {
        add => _body.OnCollision += value;
        remove => _body.OnCollision -= value;
    }
    
    private Animation Animation
    {
        get;
        set
        {
            field?.Kill();
            value.Transform.Position = _body.Position;
            value.Transform.Rotation = _body.Rotation;
            Game.AnimationsPool.Add(value);
            field = value;
        }
    }

    private double _activationTime;
    private Body _body;

    public Collider(
        GameObject go,
        Shape shape,
        Category layer,
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
        fixture.CollisionCategories = layer;
        fixture.CollidesWith = layer;
        fixture.IsSensor = isSensor;
        Collision.World.Add(_body);
        _activationTime = Time.TotalGameTime.TotalSeconds;
        
        Animation = new ColliderAnimation(_body.FixtureList, Debug.ColliderColor, 1);
    }

    public override void Update()
    {
        Animation.Transform.Position = _body.Position;
        Animation.Transform.Rotation = _body.Rotation;
        Animation.IsVisible = _body.Enabled;
    }

    public override void OnDestroyImmediate()
    {
        var timeToEnd = _activationTime + Debug.ColliderMinimalTime - Time.TotalGameTime.TotalSeconds;
        if (timeToEnd > 0)
        {
            Animation.Transform.Position = _body.Position;
            Animation.Transform.Rotation = _body.Rotation;
            Animation.TimeToKill = timeToEnd;
        }

        Collision.World.Remove(_body);
        _body = null;
    }
}

public class ColliderAnimation(
    FixtureCollection fixtureList, 
    Color color,
    float layer = 0) : Animation
{
    public override void Draw()
    {
        foreach (var fixture in fixtureList)
        {
            Game.Painter.BorderShape(layer, fixture.Shape, Transform.WorldPosition, Transform.Rotation, color);
        }
    }
}