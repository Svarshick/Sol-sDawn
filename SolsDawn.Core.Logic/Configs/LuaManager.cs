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
        BossScript = new Script(CoreModules.Basic | CoreModules.Coroutine);
        OrbScript = new Script(CoreModules.Basic | CoreModules.Coroutine);
        ScriptRoot = root ?? "";
        
        var loader = new FileSystemScriptLoader();
        loader.ModulePaths = new[]
        {
            Path.Combine(ScriptRoot, "?"),
            Path.Combine(ScriptRoot, "?.lua"),
        };
        BossScript.Options.ScriptLoader = loader;
        OrbScript.Options.ScriptLoader = loader;

        InitializeAPI();
    }

    private void InitializeAPI()
    {
        UserData.RegisterProxyType<LuaEventProxy, LuaEvent>(source => new LuaEventProxy(source));
        //UserData.RegisterProxyType<LuaEventProxy, LuaTimer>(source => new LuaEventProxy(source));
        
        const string waitCode = @"
            return function(target)
                if type(target) == 'number' then
                    target = timer(target)
                end
                while not target.isFired and not target.isCanceled do
                    coroutine.yield()
                end
            end
        ";
        
        var bossEnv = new Table(BossScript)
        {
            ["run"] = (Func<string, DynValue>)BlockRoutine,
            ["timer"] = (Func<double, LuaTimer>)CreateTimer,
            
            MetaTable = new Table(BossScript)
            {
                ["__index"] = BossScript.Globals
            },
        };

        var wait = CompileFunction(waitCode, bossEnv, BossScript);
        bossEnv["wait"] = wait;
        API[ActorType.Boss] = bossEnv;

        var orbEnv = new Table(OrbScript)
        {
            ["run"] = (Func<string, DynValue>)BlockRoutine,
            
            MetaTable = new Table(OrbScript)
            {
                ["__index"] = OrbScript.Globals
            },
        };
        API[ActorType.Orb] = orbEnv;
    }
    
    private DynValue CompileFunction(string functionFabric, Table env, Script script)
    {
        var chunk = script.LoadString(functionFabric, env);
        return script.Call(chunk);
    }
    
    private static LuaTimer CreateTimer(double delay)
    {
        var currentRoutine = LuaExecutionContext.CurrentRoutine;
        if (currentRoutine == null)
        {
            throw new InvalidOperationException("Cannot create a timer outside of an active LuaRoutine execution context.");
        }

        var timer = new LuaTimer(currentRoutine, delay);
        currentRoutine.StartTimer(timer);
        return timer;
    }

    private static DynValue BlockRoutine(string path)
    {
        var manager = LuaExecutionContext.LuaManager ?? throw new NullReferenceException("Expected not null LuaManager");
        var routine = LuaExecutionContext.CurrentRoutine ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var package = routine.Package;
        var script = manager.GetCompiledScript(Path.Combine(package, path) + ".lua");
        routine.BlockWithRoutine(script);
        return DynValue.NewYieldReq([]);
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