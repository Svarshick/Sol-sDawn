using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic;

public class GameTests : IUpdatable, IDrawable
{
    public bool IsActive = false;
    private StarBlink _starBlink;

    public GameTests()
    {
        var world = new World(Vector2.Zero);
        var shape1 = new CircleShape(10, 1.0f);
        var body1 = new Body { BodyType = BodyType.Kinematic };
        world.Add(body1);
        var fixture1 = body1.CreateFixture(shape1);
        fixture1.IsSensor = true;
        world.Step(1f / 60f);
        
        var shape2 = new CircleShape(1, 1.0f);
        var queryTransform = new nkast.Aether.Physics2D.Common.Transform(Vector2.Zero, 0f);
        shape2.ComputeAABB(out var aabb, ref queryTransform, 0);
        
        bool didIntersect = false;
        world.QueryAABB(fixture =>
        {
            fixture.Body.GetTransform(out var bodyTransform);
            if (nkast.Aether.Physics2D.Collision.Collision.TestOverlap(
                    shape2,
                    0,
                    fixture.Shape,
                    0,
                    ref queryTransform,
                    ref bodyTransform))
            {
                didIntersect = true;
            }

            return true;
        }, ref aabb);
        
        Console.WriteLine(didIntersect);
        
        if (!IsActive)
            return;
    }

    private double _lastTime;
    public void Update()
    {
         if (!IsActive)
            return;

         if (Time.TotalGameTime.TotalSeconds - _lastTime > 2)
         {
             _lastTime = Time.TotalGameTime.TotalSeconds;
             var trans = new Transform2();
             trans.Position = new Vector2(-100, -100);
             _starBlink = new StarBlink(
                 trans,
                 0.3f,
                 (float)Math.PI,
                 20,
                 200,
                 5,
                 1,
                 true,
                 Color.Yellow,
                 Color.White);
         }
    }

    public void LateUpdate()
    {
        if (!IsActive)
            return;
    }

    public void Draw()
    {
        if (!IsActive)
            return;
        
        if (_starBlink is not null && !_starBlink.IsFinished)
            _starBlink.Draw();
    }
}