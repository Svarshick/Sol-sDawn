using System;
using nkast.Aether.Physics2D;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic;
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

    public static async Job SolidVsSensorCollisionFrames()
    {
        var platform = new Platform(CreateObject(), 5);
        var solid = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic);
        var sensor = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic, true);
        solid.Transform.Position = new Vector2(-1, 1);
        sensor.Transform.Position = new Vector2(1, 1);
        platform.Collider.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} platform"); 
            return true;
        };
        
        bool solidCollided = false;
        solid.OnCollision += _ =>
        {
            solidCollided = true;
            return true;
        };
        bool sensorCollided = false;
        sensor.OnCollision += _ =>
        {
            sensorCollided = true;
            return true;
        };
        
        solid._body.LinearVelocity = new Vector2(0, -1);
        var frameStart = TotalFrames;
        while (!solidCollided)
        {
            await NextFrame();
        }

        solid._body.LinearVelocity = Vector2.Zero;
        Console.WriteLine($"{TotalFrames} Solid in: {TotalFrames - frameStart}");

        sensor._body.LinearVelocity = new Vector2(0, -1);
        frameStart = TotalFrames;
        while (!sensorCollided)
        {
            await NextFrame();
        }

        sensor._body.LinearVelocity = Vector2.Zero;
        Console.WriteLine($"{TotalFrames} Sensor in: {TotalFrames - frameStart}");
        
        while (true) 
            await NextFrame();
    }

    public static async Job SolidVsSensorCollision()
    {
        var platform = new Platform(CreateObject(), 5);
        var solid = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic);
        var sensor = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic, true);
        solid.Transform.Position = new Vector2(-1, 5);
        sensor.Transform.Position = new Vector2(1, 5);
        var downVelocity = new Vector2(0, -20); 

        await Timer(1);
        Console.WriteLine($"{TotalFrames} Start");

        bool collisionFlag = false;
        solid.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} Solid: _body {solid._body.Position} | Transform {solid.Transform.Position}");
            collisionFlag = true;
            return true;
        };
        sensor.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} Sensor: _body {sensor._body.Position} | Transform {sensor.Transform.Position}");
            collisionFlag = true;
            return true;
        };
        
        solid._body.LinearVelocity = downVelocity;
        while (!collisionFlag)
            await NextFrame();

        solid._body.LinearVelocity = Vector2.Zero;
        collisionFlag = false;
        
        sensor._body.LinearVelocity = downVelocity;
        while (!collisionFlag)
            await NextFrame();

        sensor._body.LinearVelocity = Vector2.Zero;
        collisionFlag = false;

        while (true)
            await NextFrame();
    }

    public static async Job CollisionSteps()
    {
        var platform = new Platform(CreateObject(), 5);
        var solid = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall);
        var sensor = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall, BodyType.Dynamic, true);
        solid.Transform.Position = new Vector2(-1, 5);
        sensor.Transform.Position = new Vector2(1, 5);
        var downVelocity = new Vector2(0, -20); 
        
        bool solidCollided = false;
        bool sensorCollided = false;
        solid.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} Solid: Collision. _body {solid._body.Position} | Transform {solid.Transform.Position}");
            solid._body.LinearVelocity = Vector2.Zero;
            solidCollided = true;
            return true;
        };
        solid.OnPreSolve += _ =>
        {
            Console.WriteLine($"{TotalFrames} Solid: PreSolve. _body {solid._body.Position} | Transform {solid.Transform.Position}");
            return true;
        };
        sensor.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} Sensor: Collision. _body {sensor._body.Position} | Transform {sensor.Transform.Position}");
            sensor._body.LinearVelocity = Vector2.Zero;
            sensorCollided = true;
            return true;
        };
        sensor.OnPreSolve += _ =>
        {
            Console.WriteLine($"{TotalFrames} Sensor: PreSolve. _body {sensor._body.Position} | Transform {sensor.Transform.Position}");
            return true;
        };
        
        await Timer(1);
        Console.WriteLine($"{TotalFrames} Start");
        
        solid._body.LinearVelocity = downVelocity;
        sensor._body.LinearVelocity = downVelocity;
        
        while (!sensorCollided || !sensorCollided)
            await NextFrame();

        Console.WriteLine($"{TotalFrames} {solid._body.Position}");
        Console.WriteLine($"{TotalFrames} {sensor._body.Position}");
        
        while (true)
            await NextFrame();
    }

    public static async Job MovingAlongTangentAfterColliding()
    {
        var horizontalPlatform = new Platform(CreateObject(), 5);
        var verticalPlatform = new Platform(CreateObject(), 5);
        verticalPlatform.Transform.Rotation = PI / 2;
        verticalPlatform.Transform.Position = new Vector2(2.05f, 0);
        var solid = new Core.Logic.Gameplay.Collider(CreateObject(), Shapes.Circle(0.5f), Layer.Player, Layer.Wall);
        solid.Transform.Position = new Vector2(0, 2);
        var velocity = new Vector2(20, -20);
        solid.OnPreSolve += _ =>
        {
            Console.WriteLine($"{TotalFrames} PreSolve. _body {solid._body.Position} | Transform {solid.Transform.Position}");
            return true;
        };
        solid.OnCollision += _ =>
        {
            Console.WriteLine($"{TotalFrames} Collision. _body {solid._body.Position} | Transform {solid.Transform.Position}");
            return true;
        };

        await Timer(1);
        solid._body.LinearVelocity = velocity;
        while (true)
            await NextFrame();
    }

    public static async Job CircleCast()
    {
        var platform1 = new Platform(CreateObject(), 2);
        platform1.Transform.Position = new Vector2(2.5f, 0);
        await Timer(1);
        
        var result = Core.Logic.Gameplay.Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Player, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.HitFixture.Tag == platform1.Collider} | {result.Fraction}"); //0.005 inaccuracy by Settings.LinearSlop
        }
        else
        {
            Log.Physics.Error("Should hit");
        }

        result = Core.Logic.Gameplay.Physics.CircleCast(Vector2.Zero, new Vector2(0, 2), 1, Layer.Player, Layer.Wall);
        if (!result.Hit)
        {
            Log.Physics.Info($"Shouldn't be any result"); //0.005 inaccuracy by Settings.LinearSlop
        }
        else
        {
            Log.Physics.Error("Shouldn't hit");
        }
        
        var platform2 = new Platform(CreateObject(), 2);
        platform2.Transform.Position = new Vector2(1.51f, 0);
        platform2.Transform.Rotation = PI / 2;
        await Timer(1);
        
        result = Core.Logic.Gameplay.Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Player, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.HitFixture.Tag == platform1.Collider} | {result.Fraction}");
        }
        else
        {
            Log.Physics.Error($"Should hit");
        }
        
        var platform3 = new Platform(CreateObject(), 2);
        platform3.Transform.Position = new Vector2(1 / Sqrt(2), 1 / Sqrt(2));
        platform3.Transform.Rotation = -PI / 4;
        platform2.Transform.Position = new Vector2(1.1f, 0);
        await Timer(1);
        
        result = Core.Logic.Gameplay.Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Player, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.HitFixture.Tag == platform3.Collider} | {result.Fraction}");
        }
        else
        {
            Log.Physics.Error("Should hit");
        }
        
        platform1.Destroy();
        platform2.Destroy();
        platform3.Destroy();
        var platform4 = new Platform(CreateObject(), 2);
        platform4.Transform.Position = new Vector2(-0.5f, 0);
        platform4.Transform.Rotation = -PI / 2;
        await Timer(1);
        
        result = Core.Logic.Gameplay.Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Player, Layer.Wall);
        if (!result.Hit)
        {
            Log.Physics.Info("Ok");
        }
        else
        {
            Log.Physics.Error("Shouldn't hit");
        }       

        while (true)
            await NextFrame();
    }

    public static async Job CircleCast2()
    {
        var platform = new Platform(CreateObject(), 2);
        var result = Core.Logic.Gameplay.Physics.CircleCast(new Vector2(0, 1), Vector2.Zero, 0.5f, Layer.Player, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info("OK");
        }
        else
        {
            Log.Physics.Error("Loh");
        }
    }
}