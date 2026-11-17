using System;
using System.IO;
using System.Runtime.CompilerServices;
using SolsDawn.Core.Logic.SpeedGraph;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static GameConfig Config = null!;

    public static Job RootJob()
    {
        Library.Initialize(DataDirectory());
        Config = Library.LoadRoot<GameConfig>();
        SpeedGraphs.Initialize(Path.Combine(DataDirectory(), "Graphs"));
        return LevelRunner.Run(Levels.Sandbox);
    }

    private static string DataDirectory([CallerFilePath] string sourceFile = "")
    {
#if DEBUG
        return Path.Combine(Path.GetDirectoryName(sourceFile)!, "_Data");
#else
        return Path.Combine(AppContext.BaseDirectory, "_Data");
#endif
    }
}
