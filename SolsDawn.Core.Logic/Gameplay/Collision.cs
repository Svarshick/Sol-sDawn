using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

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

public sealed class Collider : Component<Collider>
{
    public Body Body { get; private set; }
    
    public Collider(
        GameObject go, 
        Shape shape,
        Category layer, 
        BodyType bodyType = BodyType.Dynamic,
        bool isSensor = false) : base(go)
    {
        Body = new Body { Position = go.Transform.Position, Rotation = go.Transform.Rotation, BodyType = bodyType };
        Body.Tag = this;
        Fixture fixture = Body.CreateFixture(shape);
        fixture.CollisionCategories = layer;
        fixture.CollidesWith = layer; 
        fixture.IsSensor = isSensor;
        Collision.World.Add(Body);
    }

    public override void Dispose()
    {
        Collision.World.Remove(Body);
        Body = null;
    }
}