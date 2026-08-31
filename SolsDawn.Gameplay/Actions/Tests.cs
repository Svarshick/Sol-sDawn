using System;

namespace SolsDawn.Gameplay.Actions;

public static class Tests
{
    public static async Job Collider()
    {
        while (true)
        {
            var maxDistance = 5f;
            var distance = Random.Shared.NextSingle() * maxDistance;
            var angle = Random.Shared.NextSingle() * 2 * PI;
            var position = new Vector2(distance, 0).Rotated(angle);

            var pwShape = Shapes.Circle(1);
            var pw = Fight.BladeParryWindow(
                null,
                pwShape,
                position,
                0,
                _ => Job.CompletedJob);

            pw.Open();
            await Timer(1);
            pw.Destroy();
            await Timer(1);
        }
    }

    public static async Job OrbSpam()
    {
        int t = -1;
        int counter = 0;
        while (true)
        {
            t++;
            t %= 4;
            if (t == 0)
            {
                Actions.SimpleActions.SpawnOrb();
                counter++;
                Console.WriteLine($"new {counter} orb!");
            }

            await NextFrame();
        }
    }
}