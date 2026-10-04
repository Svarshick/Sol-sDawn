using System;
using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay;

public static class Test
{
    public static async Job GhostVertex()
    {
        var collider1 = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(1f), Layer.Wall, Layer.Player, BodyType.Static, true);
        collider1.Transform.Position = new Vector2(4, 0);
        var collider2 = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic, true);
        collider2._body.LinearVelocity = new Vector2(1, 0);
        
        collider2.OnCollision += d =>
        {
            Console.WriteLine("Collision");
            return true;
        };
        
        collider2.OnSeparation += _ =>
        {
            Console.WriteLine("Separation");
        };
       
        while (true)
        {
            await NextFrame();
        }
    }

    //important for edge cases analyzing
    public static async Job ShapeSkin()
    {
        var c = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.05f), Layer.Player, Layer.Wall);
        c.Transform.Position = new Vector2(6, 4);
        c._body.LinearVelocity = new Vector2(-10, 0);
        var platform = new Platform(CreateObject(), 5);
        var rigidCircle = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(1f), Layer.Player, Layer.Wall);
        var sensorCircle = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(1f), Layer.Player, Layer.Wall, BodyType.Dynamic, true);

        rigidCircle.Transform.Position = new Vector2(0, 1.02f);
        sensorCircle.Transform.Position = new Vector2(0, 1.02f);

        rigidCircle.OnCollision += _ =>
        {
            Console.WriteLine($"RigidCircle: {rigidCircle.Transform.Position}");
            return true;
        };
        sensorCircle.OnCollision += _ =>
        {
            Console.WriteLine($"SensorCircle: {sensorCircle.Transform.Position}");
            return true;
        };

        Console.WriteLine($"LinearSlope: {Settings.LinearSlop}");
        Console.WriteLine($"Platform radius: {platform.Radius}");
        
        rigidCircle._body.LinearVelocity = new Vector2(0, -0.02f);
        
        await Timer(2);
        
        sensorCircle._body.LinearVelocity = new Vector2(0, -0.02f);

        await Timer(2);
    }

    public static async Job PhysicsAligning()
    {
        var collider = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.2f), Layer.Player, Layer.Wall);
        var platform = new Platform(CreateObject(), 5);

        while (true)
        {
            if (Input.Jump)
            {
                collider._body.ApplyLinearImpulse(new Vector2(0, 5));
            }
            else
            {
                collider._body.ApplyForce(new Vector2(0, -40));
            }
            await NextFrame();
        }
    }
}