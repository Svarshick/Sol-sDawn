using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MoonSharp.Interpreter;
using SolsDawn.Core.Logic.Animations.Lua;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic.Configs;

public static class LuaAPI
{
    public static LuaTimer CreateTimer(double delay)
    {
        var routine = LuaExecutionContext.CurrentRoutine ?? throw new NullReferenceException("Cannot create a timer outside of an active LuaRoutine execution context.");
        var timer = new LuaTimer(routine, delay);
        routine.StartTimer(timer);
        return timer;
    }

    public static DynValue BlockRoutine(string path)
    {
        var manager = LuaExecutionContext.LuaLoader ?? throw new NullReferenceException("Expected not null LuaManager");
        var routine = LuaExecutionContext.CurrentRoutine ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var package = routine.Package;
        var script = manager.GetCompiledScript(Path.Combine(package, path) + ".lua");
        routine.BlockWithRoutine(script);
        return DynValue.NewYieldReq([]);
    }

    public static LuaRoutine Subroutine(DynValue callback)
    {
        var routine = LuaExecutionContext.CurrentRoutine;
        var subroutine = routine.CreateSubroutine(callback);
        routine.StartSubroutine(subroutine);
        return subroutine;
    }

    public static LuaEventRace Race(params DynValue[] args)
    {
        var racers = new List<LuaEvent>();
        foreach(var arg in args)
        {
            switch (arg.UserData?.Object)
            {
                case LuaEvent evt:
                    racers.Add(evt);
                    break;
                case LuaRoutine routine:
                    racers.Add(routine.FinishEvent);
                    break;
                default:
                    throw new ArgumentException("race() expects events or routines");
            }
        }

        var race = new LuaEventRace(LuaExecutionContext.CurrentRoutine, racers);
        return race;
    }

    public static LineIdle CreateLineAnimation(Vector2 point1, Vector2 point2, Color color, float thickness = 1, float layerDepth = 0)
    {
        var line = new LineIdle(point1, point2, thickness, color, layerDepth);
        Game.AnimationsPool.Add(line);
        return line;
    }

    public static LineTrace CreateLineTraceAnimation(
        Vector2 point1, 
        Vector2 point2, 
        float thickness, 
        float duration,
        Color startColor,
        Color endColor, 
        float layerDepth = 0)
    {
        var trace = new LineTrace(
            point1, 
            point2, 
            thickness, 
            duration, 
            startColor,
            endColor,
            layerDepth);
        Game.AnimationsPool.Add(trace);
        return trace;
    }

    public static CircleIdle CreateCircleIdleAnimation(
        Vector2 position,
        float radius,
        int sides,
        float thickness,
        Color color,
        float layerDepth = 0)
    {
        var animation = new CircleIdle(position, radius, sides, thickness, color, layerDepth);
        Game.AnimationsPool.Add(animation);
        return animation;
    }

    public static CollisionShape2D CreateCircleCollider(Vector2 center, float radius)
    {
        var shape = new BoundingCircle2D(center, radius);
        return new CollisionShape2D(shape);
    }

    public static CollisionShape2D CreateSquareCollider(Vector2 position, float a)
    {
        var shape = BoundingBox2D.CreateFromCenterAndExtents(position, new Vector2(a/2, a/2));
        return new CollisionShape2D(shape);
    }

    public static ParryWindow CreateParryWindow(
        Vector2 position,
        CollisionShape2D collider,
        DynValue determineParried)
    {
        var routine = LuaExecutionContext.CurrentRoutine 
            ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var go = new GameObject();
        go.Transform.Position = position;
        return new ParryWindow(go, collider, ParryType.Blade, routine, determineParried);
    }
}

[MoonSharpUserData]
public class ParryWindow
{
    public readonly ParryType type;
    public readonly CollisionShape2D collider;
    public readonly LuaEvent parried;
    
    private readonly GameObject _go;
    private readonly LuaRoutine _routine;
    private readonly DynValue _determineParried;

    [MoonSharpHidden]
    public ParryWindow(
        GameObject go,
        CollisionShape2D collider,
        ParryType type,
        LuaRoutine routine,
        DynValue determineParried)
    {
        this.type = type;
        this.collider = collider;
        _go = go;
        _routine = routine;
        _determineParried = determineParried;
        
        parried = new (_routine);
    }
    
    public void open()
    {
        new SolsDawn.Core.Logic.Gameplay.ParryWindow(_go, type, _routine, _determineParried, parried);
        new Collider(_go, 0, Collision.LayerName.Parry, collider);
    }

    public void close()
    {
        _go.Dispose();
    }
}