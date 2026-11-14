using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Utils;
namespace SolsDawn.Gameplay;

public static class Test
{
    public static async Job CircleCast()
    {
        var platform1 = new Platform(CreateObject(), 2);
        platform1.Transform.Position = new Vector2(2.5f, 0);
        await Timer(1);
        
        var result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform1.Collider} | {result.Fraction}"); //0.005 inaccuracy by Settings.LinearSlop
        }
        else
        {
            Log.Physics.Error("Should hit");
        }

        result = Physics.CircleCast(Vector2.Zero, new Vector2(0, 2), 1, Layer.Wall);
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
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform1.Collider} | {result.Fraction}");
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
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform3.Collider} | {result.Fraction}");
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
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
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
        var result = Physics.CircleCast(new Vector2(0, 1), Vector2.Zero, 0.5f, Layer.Wall);
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