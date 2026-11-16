namespace SolsDawn.Gameplay;

public static class Levels
{
    [Level]
    public static async Job Sandbox()
    {
        Spawn.Platform(new Vector2(0, 0), 100);
        Spawn.Platform(new Vector2(-5, 0), 10, -PI / 4);
        Spawn.Platform(new Vector2(-7, 3), 1000, -PI * 5 / 8);
        Spawn.Platform(new Vector2(0, 4), 10);
        Spawn.Platform(new Vector2(60, 10), 100, ASin(10 / 50f));

        Spawn.Player(new Vector2(6, 4), Config.Player);
        Spawn.Enemy(new Vector2(14, 1.5f), Config.Enemy, EnemyBehaviours.Duelist);

        while (true)
            await NextFrame();
    }
}
