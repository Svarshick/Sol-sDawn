using System;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace SolsDawn.Core.Logic.Configs;

public enum ActorType
{
    Boss,
    Orb
}

public class LuaManager
{
    public Script BossScript { get; }
    public Script OrbScript { get; }
    public string ScriptRoot { get; }
    private readonly Dictionary<string, DynValue> _scriptCache = new();

    public static readonly Dictionary<ActorType, Table> API = new();

    public LuaManager(string root)
    {
        BossScript = new Script(CoreModules.LoadMethods | CoreModules.Basic | CoreModules.Coroutine);
        OrbScript = new Script(CoreModules.LoadMethods | CoreModules.Basic | CoreModules.Coroutine);
        ScriptRoot = root ?? "";
        
        var loader = new FileSystemScriptLoader();
        loader.ModulePaths = new[]
        {
            Path.Combine(ScriptRoot, "?"),
            Path.Combine(ScriptRoot, "?.lua"),
        };
        BossScript.Options.ScriptLoader = loader;
        OrbScript.Options.ScriptLoader = loader;

        InitializeApis();
    }
    
    private void InitializeApis()
    {
        var bossEnv = new Table(BossScript)
        {
            ["wait"] = DynValue.NewCallback((_, _) => DynValue.NewYieldReq([])),
            
            ["attack"] = DynValue.NewCallback((_, args) =>
            {
                Console.WriteLine($"{(int)LuaExecutionContext.CurrentActor} attack. Status {args[0].String}");
                return DynValue.NewYieldReq([]);
            }),

            ["status"] = DynValue.NewString("init status"),
            
            MetaTable = new Table(BossScript)
            {
                ["__index"] = BossScript.Globals
            },
        };
        API[ActorType.Boss] = bossEnv;

        var orbEnv = new Table(OrbScript)
        {
            ["fly"] = DynValue.NewCallback((_, _) =>
            {
                Console.WriteLine("Orb flies!");
                return DynValue.NewYieldReq([]);
            }),
            
            MetaTable = new Table(OrbScript)
            {
                ["__index"] = OrbScript.Globals
            },
        };
        API[ActorType.Orb] = orbEnv;
    }

    public DynValue GetCompiledScript(string path)
    {
        if (!_scriptCache.TryGetValue(path, out var compiledFunc))
        {
            var absolutePath = Path.Combine(ScriptRoot, path);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Script file not found: {absolutePath}");
            }

            var code = File.ReadAllText(absolutePath);

            ActorType actorType;
            if (path.StartsWith("boss/", StringComparison.OrdinalIgnoreCase))
            {
                actorType = ActorType.Boss;
                compiledFunc = BossScript.LoadString(code, API[actorType], absolutePath);
                _scriptCache[path] = compiledFunc;
            }
            else if (path.StartsWith("orb/", StringComparison.OrdinalIgnoreCase))
            {
                actorType = ActorType.Orb;
                compiledFunc = OrbScript.LoadString(code, API[actorType], absolutePath);
                _scriptCache[path] = compiledFunc;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine ActorType from path '{path}'. " +
                    "Expected path to start with 'boss/' or 'orb/'."
                );
            }
        }

        return compiledFunc;
    }
}