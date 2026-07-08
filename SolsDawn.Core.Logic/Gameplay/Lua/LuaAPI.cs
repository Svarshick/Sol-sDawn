using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations.Lua;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

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
        var script = manager.GetCompiledScript(path + ".lua");
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

    public static Shape CreateCircle(float radius)
    {
        return new CircleShape(radius, 1.0f);
    }

    public static Shape CreateRectangle(float width, float height)
    {
        var vertices = PolygonTools.CreateRectangle(width/2, height/2);
        return new PolygonShape(vertices, 1.0f);
    }

    public static Shape CreateSquare(float side) => CreateRectangle(side, side);

    public static ParryWindow CreateParryWindow(
        //ParryType type,
        Shape shape,
        DynValue parryExecuter,
        DynValue parryDeterminer)
    {
        var routine = LuaExecutionContext.CurrentRoutine 
            ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var go = new GameObject();
        new Collider(go, shape, Collision.Parry, BodyType.Kinematic, true);
        return new ParryWindow(go, ParryType.Blade, routine, parryExecuter, parryDeterminer);
    }
}