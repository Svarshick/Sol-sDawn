namespace SolsDawn.Gameplay;

public static class Main
{
    public static GameConfig Config = null!;

    public static Job RootJob()
    {
        InitializeLibrary(DataDirectory());
        Config = LoadRoot<GameConfig>();
        InitializeSpeedGraphs(DataDirectory("Graphs"));
        return RunLevels(Levels.Sandbox, Config.Camera);
    }
}
