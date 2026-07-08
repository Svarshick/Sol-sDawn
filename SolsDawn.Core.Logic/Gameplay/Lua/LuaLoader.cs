using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using nkast.Aether.Physics2D.Collision.Shapes;
using SolsDawn.Core.Logic.Animations.Lua;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

public class LuaLoader
{
    public Script Script { get; }
    public string ScriptRoot { get; }
    private readonly Dictionary<string, DynValue> _scriptCache = new();

    public static Table API { get; private set; }

    public LuaLoader(string root)
    {
        Script = new Script(CoreModules.Basic | CoreModules.Coroutine);
        ScriptRoot = root ?? "";
        
        var loader = new FileSystemScriptLoader();
        loader.ModulePaths = new[]
        {
            Path.Combine(ScriptRoot, "?"),
            Path.Combine(ScriptRoot, "?.lua"),
        };
        Script.Options.ScriptLoader = loader;

        InitializeAPI();
    }

    private void InitializeAPI()
    {
        UserData.RegisterType<LuaEvent>();
        UserData.RegisterProxyType<LuaEvent, LuaTimer>(timer => timer);
        UserData.RegisterType<LuaRoutine>();
        UserData.RegisterType<LuaEventRace>();
        
        UserData.RegisterType<Vector2>();
        UserData.RegisterType<Transform2>();
        UserData.RegisterType<Color>();

        UserData.RegisterType<HP>();
        
        UserData.RegisterType<Shape>();
        UserData.RegisterType<ParryWindow>();

        UserData.RegisterType<CircleIdle>();
        UserData.RegisterType<LineIdle>();
        UserData.RegisterType<LineTrace>();
            
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

        var shape = new Table(Script)
        {
            ["circle"] = (Func<float, Shape>)LuaAPI.CreateCircle,
            ["rectangle"] = (Func<float, float, Shape>)LuaAPI.CreateRectangle,
            ["square"] = (Func<float, Shape>)LuaAPI.CreateSquare,
        };

        var animations = new Table(Script)
        {
            ["line"] = (Func<Vector2, Vector2, Color, float, float, LineIdle>)LuaAPI.CreateLineAnimation,
            ["lineTrace"] = (Func<Vector2, Vector2, float, float, Color, Color, float, LineTrace>)LuaAPI.CreateLineTraceAnimation,
            ["circle"] = (Func<Vector2, float, int, float, Color, float, CircleIdle>)LuaAPI.CreateCircleIdleAnimation,
        };

        var env = new Table(Script)
        {
            ["run"] = (Func<string, DynValue>)LuaAPI.BlockRoutine,
            ["subroutine"] = (Func<DynValue, LuaRoutine>)LuaAPI.Subroutine,
            
            ["timer"] = (Func<double, LuaTimer>)LuaAPI.CreateTimer,
            ["race"] = (Func<DynValue[], LuaEventRace>)LuaAPI.Race,

            ["vector"] = (float x, float y) => new Vector2(x, y),
            ["rotate"] = (Vector2 vector, float radians) => Vector2.Rotate(vector, radians),
            ["color"] = (int r, int g, int b, int a = 256) => new Color(r, g, b, a),
            
            ["transform"] = () => LuaExecutionContext.CurrentRoutine.Actor.Transform,
            ["hp"] = () => LuaExecutionContext.CurrentRoutine.Actor.HP,
            
            ["shape"] = shape,
            ["parryWindow"] = (Func<Shape, DynValue, DynValue, ParryWindow>)LuaAPI.CreateParryWindow,
            ["animation"] = animations,

            MetaTable = new Table(Script)
            {
                ["__index"] = DynValue.FromObject(Script,
                    (Func<Table, DynValue, DynValue>)((t, k) =>
                    {
                        if (k.String == "boss_position")
                        {
                            var pos = IntentionsPool.Blackboard.Boss.GameObject.Transform.Position;
                            return DynValue.FromObject(Script, pos);
                        }

                        var ownValue = t.RawGet(k);
                        if (DynValue.Nil.Equals(ownValue))
                            return ownValue;
                        return Script.Globals.RawGet(k) ?? DynValue.Nil;
                    })),

                ["__newindex"] = DynValue.FromObject(Script,
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

        var wait = CompileFunction(waitCode, env, Script);
        env["wait"] = wait;
        API = env;
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

            compiledFunc = Script.LoadString(code, API, absolutePath);
            _scriptCache[path] = compiledFunc;
        }

        return compiledFunc;
    }
}