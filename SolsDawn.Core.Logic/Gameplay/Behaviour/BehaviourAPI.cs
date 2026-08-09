using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations.Lua;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public static class BehaviourAPI
{
    public static float Angle(this Vector2 v) => (float)Math.Atan2(v.Y, v.X);
    
    public static Routine.NextFrameAwaiter NextFrame() => new();

    public static Routine Subroutine(Callback callback)
    {
        var routine = ExecutionContext.CurrentRoutine;
        var subroutine = new Routine(callback);
        routine.StartSubroutine(subroutine);
        return subroutine;
    }
    
    public static Timer Timer(double delay)
    {
        var routine = ExecutionContext.CurrentRoutine ?? throw new NullReferenceException("Cannot create a timer outside of an active LuaRoutine execution context.");
        var timer = new Timer(routine, delay);
        routine.StartTimer(timer);
        return timer;
    }
    
    public static EventRace Race(params object[] args)
    {
        var racers = new List<Event>();
        foreach(var arg in args)
        {
            switch (arg)
            {
                case Event evt:
                    racers.Add(evt);
                    break;
                case Routine routine:
                    racers.Add(routine.Completed);
                    break;
                default:
                    throw new ArgumentException("race() expects events or routines");
            }
        }

        var race = new EventRace(ExecutionContext.CurrentRoutine, racers);
        return race;
    }

    public static ParryWindow ParryWindow(
        Shape shape,
        Vector2 position,
        float rotation,
        ParryReaction parryExecuter,
        ParryPredicate parryDeterminer)
    {
        var routine = ExecutionContext.CurrentRoutine
                      ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Parry, BodyType.Dynamic, true);
        return new ParryWindow(go, ParryType.Blade, routine, parryExecuter, parryDeterminer);
    }

    public static PlayerAttack PlayerAttack(
        Shape shape,
        Vector2 position,
        float rotation,
        HitByPlayerReaction hitExecuter,
        HitByPlayerPredicate hitDeterminer,
        ParryReaction parryExecuter)
    {
        var routine = ExecutionContext.CurrentRoutine
                      ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Enemy | Collision.Parry, BodyType.Dynamic, true);
        return new PlayerAttack(go, AttackType.Blade, routine, hitExecuter, hitDeterminer, parryExecuter);
    }

    public static EnemyAttack EnemyAttack(
        Shape shape,
        Vector2 position,
        float rotation,
        HitByEnemyReaction hitExecuter,
        HitByEnemyPredicate hitDeterminer)
    {
        var routine = ExecutionContext.CurrentRoutine
                      ?? throw new NullReferenceException("Expected not null CurrentRoutine");
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Player, BodyType.Dynamic, true);
        return new EnemyAttack(go, AttackType.Blade, routine, hitExecuter, hitDeterminer);
    }
    
    public static class Animations
    {
        public static LineTrace LineTrace(
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

        public static CircleIdle CircleIdle(
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
    }

    public static class Shapes
    {
        public static Shape Circle(float radius)
        {
            return new CircleShape(radius, 1.0f);
        }

        public static Shape Rectangle(float width, float height)
        {
            var vertices = PolygonTools.CreateRectangle(width / 2, height / 2);
            return new PolygonShape(vertices, 1.0f);
        }

        public static Shape Square(float side) => Rectangle(side, side);
    }
}