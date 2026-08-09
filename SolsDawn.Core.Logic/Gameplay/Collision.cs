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

public delegate void FixtureDrawer(FixtureCollection fixtureList, Vector2 position, float rotation);

public sealed class Collider : Component<Collider>, IDrawable
{
    public bool Awake
    {
        get => _body.Awake;
        set
        {
            if (value && !_body.Awake)
            {
                _activationTime = Time.TotalGameTime.TotalSeconds;
            }

            _body.Awake = value;
        }
    }

    public event OnCollisionEventHandler OnCollision
    {
        add => _body.OnCollision += value;
        remove => _body.OnCollision -= value;
    }
    
    public FixtureCollection FixtureList => _body.FixtureList;
    
    private FixtureDrawer DebugDrawer { get; set; }
    
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
        
        DebugDrawer = DefaultDraw;
    }

    public override void Dispose()
    {
        var timeToEnd = _activationTime + Debug.ColliderMinimalTime - Time.TotalGameTime.TotalSeconds;
        if (timeToEnd > 0)
        {
            var fixtureList = _body.FixtureList;
            var position = _body.Position;
            var rotation = _body.Rotation;
            var drawer = DebugDrawer;

            Game.AnimationsPool.Add(new DelegatedAnimation(
                () => drawer(fixtureList, position, rotation),
                (float)timeToEnd
            ));
        }

        Collision.World.Remove(_body);
        _body = null;
    }

    public static void DefaultDraw(FixtureCollection fixtureList, Vector2 position, float rotation)
    {
        if (!Debug.ColliderEnabled)
            return;
        Game.SpriteBatch.DrawFixtures(fixtureList, position, rotation, Debug.ColliderColor);
    }

    public void Draw()
    {
        if (_body.Awake)
        {
            DebugDrawer(_body.FixtureList, _body.Position, _body.Rotation);
        }
    }
}