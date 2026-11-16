using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SolsDawn.Core.Logic.Combat;
using SolsDawn.Core.Logic.Entities;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Levels;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LevelAttribute : Attribute;

public sealed record LevelInfo(string Name, JobMethod Start);

public static class G
{
    public static Player? Player;
}

public static class LevelRunner
{
    private static JobMethod? _requested;
    private static JobMethod? _currentMethod;
    private static Job? _current;

    public static IReadOnlyList<LevelInfo> Levels { get; private set; } = [];
    public static string? CurrentName { get; private set; }

    public static async Job Run(JobMethod start)
    {
        Levels = FindLevels();
        ImGuiDrawer = DebugWindow.Draw;
        _requested = start;

        Effects.Run();
        PlayerPhase();
        LevelHost();
        AfterPhase();

        while (true)
            await NextFrame();
    }

    public static void Start(JobMethod level) => _requested = level;

    public static void Restart()
    {
        if (_currentMethod is not null)
            _requested = _currentMethod;
    }

    private static async Job PlayerPhase()
    {
        while (true)
        {
            if (G.Player is { IsDestroyed: false } player)
                player.Controller.Update();
            await NextFrame();
        }
    }

    private static async Job LevelHost()
    {
        while (true)
        {
            if (_requested is { } next)
            {
                _requested = null;
                _current?.Kill();
                _currentMethod = next;
                CurrentName = next.Method.Name;
                Log.Logic.Info($"Level {CurrentName} started");
                _current = next();
            }

            await NextFrame();
        }
    }

    private static async Job AfterPhase()
    {
        while (true)
        {
            CombatSystem.Resolve();
            if (G.Player is { IsDestroyed: false } player)
                Camera.Position = player.Transform.Position;
            await NextFrame();
        }
    }

    private static List<LevelInfo> FindLevels()
    {
        var levels = new List<LevelInfo>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.GetCustomAttribute<LevelAttribute>() == null)
                        continue;

                    if (!method.IsPublic || !method.IsStatic || method.GetParameters().Length != 0 || method.ReturnType != typeof(Job))
                    {
                        Log.Logic.Error($"Level {type.Name}.{method.Name} must be public static, take no parameters and return Job.");
                        continue;
                    }

                    levels.Add(new LevelInfo(method.Name, method.CreateDelegate<JobMethod>()));
                }
            }
        }

        return levels.OrderBy(l => l.Name, StringComparer.Ordinal).ToList();
    }
}
