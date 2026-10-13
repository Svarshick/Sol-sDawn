using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Physics
{
    private static readonly World World;

    static Physics()
    {
        World = new World(Vector2.Zero);
        World.ContactManager.PreSolve += OnPreSolve;
    }

    public static void Add(Body body) => World.Add(body);
    public static void Remove(Body body) => World.Remove(body);
    
    public static void Step(GameTime gameTime)
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

        //using Transform.Position is safe in EVERY point
        //[can access][SAVE] before World.Step body.Position = Transform.Position 
        //[can access][SAVE] in World.Step at OnCollision/OnSeparation are called, but body.Position isn't changed yet
        //[can't access][NOT SAVE] in World.Step after OnCollision/OnSepatation body.Position is changed
        //[can access][SAVE] after World.Step Transform.Position = body.Position
        foreach (var body in World.BodyList)
        {
            if (body.Tag is not Collider collider)
                continue;
            collider.GameObject.Transform.Position = body.Position;
            collider.GameObject.Transform.Rotation = body.Rotation;
        }
    }

    private static void OnPreSolve(Contact contact, ref Manifold oldManifold)
    {
        if (contact.FixtureA.Tag is not Collider collA || 
            contact.FixtureB.Tag is not Collider collB) 
            return;

        var applyPhysics = collA.InvokePreSolve(new CollisionData(contact.FixtureA, contact.FixtureB, contact)) &&
                           collB.InvokePreSolve(new CollisionData(contact.FixtureB, contact.FixtureA, contact));

        contact.Enabled = contact.Enabled && applyPhysics;
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
        var queryTransform = new Transform(position, rotation);
        shape.ComputeAABB(out var aabb, ref queryTransform, 0);

        World.QueryAABB(fixture =>
        {
            if ((fixture.CollisionCategories & layer) != 0)
            {
                fixture.Body.GetTransform(out var bodyTransform);

                if (Collision.TestOverlap(
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
    
    public static CircleCastResult CircleCast(
        Vector2 from, 
        Vector2 to, 
        float radius, 
        Category selfLayer = Category.All, 
        Category collidesLayer = Category.All, 
        short group = 0,
        CircleCastFixtureFilter? filter = null)
    {
        var bestHit = new CircleCastResult { Hit = false, Fraction = 1f };

        AABB movementBounds;
        Vector2.Min(ref from, ref to, out movementBounds.LowerBound);
        Vector2.Max(ref from, ref to, out movementBounds.UpperBound);
        movementBounds.LowerBound -= new Vector2(radius, radius);
        movementBounds.UpperBound += new Vector2(radius, radius);
        
        World.QueryAABB(fixture =>
        {
            var body = fixture.Body;
            if (body.BodyType != BodyType.Static)
                return true;
            
            if (group != 0 && group == fixture.CollisionGroup)
            {
                if (group < 0)
                    return true;
            }
            else
            {
                if ((fixture.CollisionCategories & collidesLayer) == Category.None ||
                    (fixture.CollidesWith & selfLayer) == Category.None)
                    return true;
            }

            var bodyWorldCenter = body.GetWorldPoint(body.LocalCenter);
            var toiInput = new TOIInput
            {
                ProxyA = new DistanceProxy(new CircleShape(radius, 1f), 0),
                SweepA = new Sweep
                {
                    C0 = from,
                    C = to,
                    LocalCenter = Vector2.Zero,
                    A0 = 0f,
                    A = 0f,
                    Alpha0 = 0f,
                },
                SweepB = new Sweep
                {
                    C0 = bodyWorldCenter,
                    C = bodyWorldCenter,
                    LocalCenter = body.LocalCenter,
                    A0 = body.Rotation,
                    A = body.Rotation,
                    Alpha0 = 0f
                },
                TMax = 1
            };

            for (int childIndex = 0; childIndex < fixture.Shape.ChildCount; childIndex++)
            {
                toiInput.ProxyB = new DistanceProxy(fixture.Shape, childIndex);
                TimeOfImpact.CalculateTimeOfImpact(out var toiOutput, ref toiInput);
                if (toiOutput.State == TOIOutputState.Touching && toiOutput.T < bestHit.Fraction)
                {
                    if (filter?.Invoke(from, to, radius, toiOutput.T, fixture) == false)
                        continue;
                    bestHit.Hit = true;
                    bestHit.Fraction = toiOutput.T;
                    bestHit.HitFixture = fixture;
                }
            }
            
            return true;
        }, ref movementBounds);

        return bestHit;
    }
}

public delegate bool CircleCastFixtureFilter(Vector2 from, Vector2 to, float radius, float fraction, Fixture other);

public struct CircleCastResult
{
    public bool Hit;
    public float Fraction; // 0.0 to 1.0 (How far along the path it hit)
    public Fixture HitFixture;
}

public record struct CollisionData(Fixture SenderFixture, Fixture OtherFixture, Contact Contact)
{
    public Collider Sender => SenderFixture.Tag as Collider ?? throw Log.Physics.FatalException("Fixture tag must be Collider");
    public Collider Other => OtherFixture.Tag as Collider ?? throw Log.Physics.FatalException("Fixture tag must be Collider");
}

public delegate bool OnCollisionHandler(CollisionData data);
public delegate void OnSeparationHandler(CollisionData data);
public delegate bool OnPreSolveHandler(CollisionData data);

public sealed class Collider : Component
{
    public event OnCollisionHandler? OnCollision; //using Transform.Position is safe
    public event OnSeparationHandler? OnSeparation;
    
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
    public Body _body; //SHOULD BE PRIVATE. TEMPORARY FOR TESTS

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
        _body.OnCollision += HandleBodyCollision;
        _body.OnSeparation += HandleBodySeparation;
        Fixture fixture = _body.CreateFixture(shape);
        fixture.Tag = this;
        fixture.CollisionCategories = selfLayer;
        fixture.CollidesWith = collidesLayer;
        fixture.IsSensor = isSensor;
        Physics.Add(_body);
        _activationTime = Time.TotalGameTime.TotalSeconds;
        
        
        Animation = new ColliderAnimation(Debug.Collider.Category.Default, _body.FixtureList, 1);
    }

    private bool HandleBodyCollision(Fixture sender, Fixture other, Contact collision)
    {
        if (OnCollision is null)
            return true;
        
        var data = new CollisionData(sender, other, collision);
        bool result = true;
        foreach (OnCollisionHandler invocation in OnCollision.GetInvocationList())
        {
            result &= invocation(data);
            if (!result)
                return result;
        }

        return result;
    }

    private void HandleBodySeparation(Fixture sender, Fixture other, Contact collision)
    {
        OnSeparation?.Invoke(new CollisionData(sender, other, collision));
    }
    
    public event OnPreSolveHandler? OnPreSolve;

    internal bool InvokePreSolve(CollisionData data)
    {
        if (OnPreSolve == null) return true;

        var result = true;
        foreach (OnPreSolveHandler invocation in OnPreSolve.GetInvocationList())
        {
            result &= invocation(data);
        }
        
        return result;
    }

    public override void Update()
    {
        Animation.Transform.Position = Transform.Position;
        Animation.Transform.Rotation = Transform.Rotation;
        Animation.IsVisible = _body.Enabled;
    }

    public override void OnDestroyImmediate()
    {
        float minimalTime = Animation.Category switch
        {
            Debug.Collider.Category.Default => Debug.Collider.DefaultMinimalTime,
            Debug.Collider.Category.Attack => Debug.Collider.AttackMinimalTime,
            Debug.Collider.Category.Parry => Debug.Collider.ParryMinimalTime,
            _ => throw Log.Physics.FatalException()
        };
        
        var timeToEnd = _activationTime + minimalTime - Time.TotalGameTime.TotalSeconds;
        if (timeToEnd > 0)
        {
            Animation.Transform.Position = Transform.Position;
            Animation.Transform.Rotation = Transform.Rotation;
            Animation.TimeToKill = timeToEnd;
        }
        else
        {
            Animation.Kill();
        }

        Physics.Remove(_body);
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
                throw Log.Physics.FatalException();
        }
        
        foreach (var fixture in FixtureList)
        {
            Game.Painter.BorderShape(Layer, fixture.Shape, Transform.WorldPosition, Transform.Rotation, color, thickness);
        }
    }
}