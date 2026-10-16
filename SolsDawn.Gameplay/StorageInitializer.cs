using System.IO;
using System.Runtime.CompilerServices;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay;

internal static class GameplayLibraryInitializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        ConfigureGameplayDataDirectory();
    }

    private static void ConfigureGameplayDataDirectory([CallerFilePath] string callerFilePath = "")
    {
        if (string.IsNullOrWhiteSpace(callerFilePath))
            return;

        // callerFilePath is SolsDawn.Gameplay/Configs/PlayerConfigs.cs -> parent is SolsDawn.Gameplay
        var configsDir = Path.GetDirectoryName(callerFilePath);
        var gameplayProjectDir = configsDir != null ? Directory.GetParent(configsDir)?.FullName : null;

        if (!string.IsNullOrWhiteSpace(gameplayProjectDir) && Directory.Exists(gameplayProjectDir))
        {
            var dataDir = Path.Combine(gameplayProjectDir, "_Data");
            Library.ConfigureStorageDirectory(dataDir);
        }
    }
}