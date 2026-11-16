using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static GameConfig Config = null!;

    public static Job RootJob()
    {
        Library.Initialize(DataDirectory());
        Config = Library.LoadRoot<GameConfig>();
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
