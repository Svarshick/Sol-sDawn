using System;
using System.IO;
using System.Text.Json;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay.Configs;

public static class PlayerConfigs
{
    public static string ConfigFilePath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "PlayerConfig.json");

    public static PlayerSpecs Load()
    {
        // Step 5: Boot & load all .json files from /Presets/ into Global Library cache
        Library.EnsureInitialized();

        PlayerSpecs? specs = null;

        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                specs = JsonSerializer.Deserialize<PlayerSpecs>(json, Library.JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Logic.Error($"Failed to deserialize PlayerConfig.json: {ex.Message}. Falling back to defaults.");
            }
        }

        specs ??= new PlayerSpecs();

        // Step 6: Resolution - bind CategoryAttributes and resolve all CategorySlot GUIDs against Library
        CategorySystem.BindAndResolveRecursive(specs);

        if (!File.Exists(ConfigFilePath))
        {
            Save(specs);
        }

        return specs;
    }

    public static void Save(PlayerSpecs specs)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(specs, Library.JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Logic.Error($"Failed to save PlayerConfig.json: {ex.Message}");
        }
    }
}