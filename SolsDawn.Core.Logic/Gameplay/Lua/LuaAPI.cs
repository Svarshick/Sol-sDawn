using System;
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

public class LuaAPI
{
    public Script Script { get; }
    public Table API { get; private set; }
    
    private readonly LuaAPIFunctions _functions;
    private readonly LuaInput _input;

    public LuaAPI(string root, Input input)
    {
        Script = new Script(CoreModules.Basic | CoreModules.Coroutine | CoreModules.LoadMethods);
        var loader = new FileSystemScriptLoader();
        loader.ModulePaths = new[]
        {
            Path.Combine(root, "?"),
            Path.Combine(root, "?.lua"),
        };
        Script.Options.ScriptLoader = loader;

        _input = new LuaInput(input);
        _functions = new();
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

        UserData.RegisterType<LuaInput>();
        UserData.RegisterType<LuaAction>();
        UserData.RegisterType<LuaTeleportAction>();

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
            ["circle"] = (Func<float, Shape>)_functions.CreateCircle,
            ["rectangle"] = (Func<float, float, Shape>)_functions.CreateRectangle,
            ["square"] = (Func<float, Shape>)_functions.CreateSquare,
        };

        var animations = new Table(Script)
        {
            ["line"] = (Func<Vector2, Vector2, Color, float, float, LineIdle>)_functions.CreateLineAnimation,
            ["lineTrace"] =
                (Func<Vector2, Vector2, float, float, Color, Color, float, LineTrace>)_functions
                    .CreateLineTraceAnimation,
            ["circle"] =
                (Func<Vector2, float, int, float, Color, float, CircleIdle>)_functions.CreateCircleIdleAnimation,
        };

        Script.Globals["subroutine"] = (Func<DynValue, LuaRoutine>)_functions.Subroutine;
        Script.Globals["get_time"] = () => Time.ElapsedGameTime.TotalSeconds;
        Script.Globals["timer"] = (Func<double, LuaTimer>)_functions.CreateTimer;
        Script.Globals["race"] = (Func<DynValue[], LuaEventRace>)_functions.Race;
        Script.Globals["input"] = _input;
        Script.Globals["vector"] = (float x, float y) => new Vector2(x, y);
        Script.Globals["rotate"] = (Vector2 vector, float radians) => Vector2.Rotate(vector, radians);
        Script.Globals["color"] = (int r, int g, int b, int a = 257) => new Color(r, g, b, a);
        Script.Globals["shape"] = shape;
        Script.Globals["parryWindow"] = (Func<Shape, DynValue, DynValue, ParryWindow>)_functions.CreateParryWindow;
        Script.Globals["animation"] = animations;

        Script.Globals.MetaTable = new Table(Script)
        {
            ["__index"] = DynValue.FromObject(Script,
                (Func<Table, DynValue, DynValue>)((t, k) =>
                {
                    if (k.String == "boss_position")
                    {
                        var pos = IntentionsPool.Blackboard.Boss.GameObject.Transform.Position;
                        return DynValue.FromObject(Script, pos);
                    }

                    return DynValue.Nil;
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
        };

        var wait = CompileFunction(waitCode, Script.Globals, Script);
        Script.Globals["wait"] = wait;

        API = Script.Globals;
    }

    private static DynValue CompileFunction(string functionFabric, Table env, Script script)
    {
        var chunk = script.LoadString(functionFabric, env);
        return script.Call(chunk);
    }
}