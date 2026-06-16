using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using SolsDawn.Core.Logic.Animations.Lua;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SolsDawn.Core.Logic.Configs;

public enum ActorType
{
    Boss,
    Orb
}

public class LuaLoader
{
    public Script BossScript { get; }
    public Script OrbScript { get; }
    public string ScriptRoot { get; }
    private readonly Dictionary<string, DynValue> _scriptCache = new();

    public static readonly Dictionary<ActorType, Table> API = new();

    public LuaLoader(string root)
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
        UserData.RegisterType<LuaEvent>();
        UserData.RegisterProxyType<LuaEvent, LuaTimer>(timer => timer);
        UserData.RegisterType<LuaRoutine>();
        UserData.RegisterType<LuaEventRace>();
        
        UserData.RegisterType<Vector2>();
        UserData.RegisterType<Color>();
        UserData.RegisterType<LineIdle>();
        UserData.RegisterType<LineTrace>();
        UserData.RegisterType<CollisionShape2D>();
        UserData.RegisterType<ParryWindow>();

        UserData.RegisterType<CircleIdle>();
        
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

        var bossCollider = new Table(BossScript)
        {
            ["circle"] = (Func<Vector2, float, CollisionShape2D>)LuaAPI.CreateCircleCollider,
            ["square"] = (Func<Vector2, float, CollisionShape2D>)LuaAPI.CreateSquareCollider,
        };

        var bossAnimations = new Table(BossScript)
        {
            ["line"] = (Func<Vector2, Vector2, Color, float, float, LineIdle>)LuaAPI.CreateLineAnimation,
            ["lineTrace"] = (Func<Vector2, Vector2, float, float, Color, Color, float, LineTrace>)LuaAPI.CreateLineTraceAnimation,
            ["circle"] = (Func<Vector2, float, int, float, Color, float, CircleIdle>)LuaAPI.CreateCircleIdleAnimation,
        };

        var bossEnv = new Table(BossScript)
        {
            ["run"] = (Func<string, DynValue>)LuaAPI.BlockRoutine,
            ["subroutine"] = (Func<DynValue, LuaRoutine>)LuaAPI.Subroutine,
            ["timer"] = (Func<double, LuaTimer>)LuaAPI.CreateTimer,
            ["race"] = (Func<DynValue[], LuaEventRace>)LuaAPI.Race,

            ["vector"] = (float x, float y) => Game.ScreenLayout.ToPixels(new Vector2(x, y)),
            ["rotate"] = (Vector2 vector, float radians) => Vector2.Rotate(vector, radians),
            ["color"] = (int r, int g, int b, int a = 256) => new Color(r, g, b, a),

            ["collider"] = bossCollider,
            ["parryWindow"] = (Func<Vector2, CollisionShape2D, DynValue, ParryWindow>)LuaAPI.CreateParryWindow,
            ["animation"] = bossAnimations,

            MetaTable = new Table(BossScript)
            {
                ["__index"] = DynValue.FromObject(BossScript,
                    (Func<Table, DynValue, DynValue>)((t, k) =>
                    {
                        if (k.String == "boss_position")
                        {
                            var pos = IntentionsPool.Blackboard.Boss.GameObject.Transform.Position;
                            return DynValue.FromObject(BossScript, pos);
                        }

                        var ownValue = t.RawGet(k);
                        if (DynValue.Nil.Equals(ownValue))
                            return ownValue;
                        return BossScript.Globals.RawGet(k) ?? DynValue.Nil;
                    })),

                ["__newindex"] = DynValue.FromObject(BossScript,
                    (Action<Table, DynValue, DynValue>)((t, k, v) =>
                    {
                        if (k.String == "boss_position")
                        {
                            var transform = IntentionsPool.Blackboard.Boss.GameObject.Transform;
                            transform.Position = v.ToObject<Vector2>();
                        }
                        else
                        {
                            t.Set(k, v);
                        }
                    }))
            }
        };

        var wait = CompileFunction(waitCode, bossEnv, BossScript);
        bossEnv["wait"] = wait;
        API[ActorType.Boss] = bossEnv;

        var orbEnv = new Table(OrbScript)
        {
            ["run"] = (Func<string, DynValue>)LuaAPI.BlockRoutine,
            
            MetaTable = new Table(OrbScript)
            {
                ["__index"] = OrbScript.Globals
            },
        };
        API[ActorType.Orb] = orbEnv;
    }
    
    private static DynValue CompileFunction(string functionFabric, Table env, Script script)
    {
        var chunk = script.LoadString(functionFabric, env);
        return script.Call(chunk);
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