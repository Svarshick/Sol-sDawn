using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public static class GameplayAPI
{
    public static CartesianCamera Camera { get; internal set; }
    public static Painter Painter { get; internal set; }
    public static Input Input { get; internal set; }
    public static AnimationsPool AnimationsPool { get; internal set; }
    public static IntentionsPool IntentionsPool { get; internal set; }
    
    private static Job GetCurrentJob() => JobContext.CurrentJob ?? throw new NullReferenceException("Current Job is null");
    
    public static GameObject CreateObject()
    {
        return new GameObject();
    }
    
    /*public static Entity CreateEntity(object stats, AnimationPlayer animationPlayer)
    {
        var go = new GameObject();
        var shape = Shapes.Rectangle(stats.Width, stats.Height);
        new Collider(go, shape, Collision.Enemy);
        new Animator<EntityAnimations>(go, new EntityAnimations(stats));
        return new Entity(go, stats);
    }*/

    public static float Angle(this Vector2 v) => (float)Math.Atan2(v.Y, v.X);
    
    public static YieldAwaiter NextFrame() => new(GetCurrentJob());

    public static Timer Timer(double delay)
    {
        var job = GetCurrentJob();
        var timer = new Timer(job, delay);
        job.StartTimer(timer);
        return timer;
    }

    public static Event Event()
    {
        var job = GetCurrentJob();
        var @event = new Event(job);
        job.TrackResource(@event);
        return @event;
    }

    public static float ElapsedSeconds => (float)Time.ElapsedGameTime.TotalSeconds;
    
    public static float TotalSeconds => (float)Time.TotalGameTime.TotalSeconds;
    
    
    public static EventRace Race(params object[] args)
    {
        var job = GetCurrentJob();
        var racers = new List<Event>();
        foreach(var arg in args)
        {
            switch (arg)
            {
                case Event evtArg:
                    racers.Add(evtArg);
                    break;
                case Job jobArg:
                    racers.Add(jobArg.Completed);
                    break;
                default:
                    throw new ArgumentException("race() expects events or routines");
            }
        }

        var race = new EventRace(job, racers);
        job.TrackResource(race);
        return race;
    }

    public static ParryWindow ParryWindow(
        Shape shape,
        Vector2 position,
        float rotation,
        ParryReaction parryExecuter,
        ParryPredicate parryDeterminer)
    {
        var job = GetCurrentJob();
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Parry, BodyType.Dynamic, true);
        job.TrackResource(go);
        return new ParryWindow(go, ParryType.Blade, job, parryExecuter, parryDeterminer);
    }

    public static PlayerAttack PlayerAttack(
        Shape shape,
        Vector2 position,
        float rotation,
        HitByPlayerReaction hitExecuter,
        HitByPlayerPredicate hitDeterminer,
        ParryReaction parryExecuter)
    {
        var job = GetCurrentJob();
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Enemy | Collision.Parry, BodyType.Dynamic, true);
        job.TrackResource(go);
        return new PlayerAttack(go, AttackType.Blade, job, hitExecuter, hitDeterminer, parryExecuter);
    }

    public static EnemyAttack EnemyAttack(
        Shape shape,
        Vector2 position,
        float rotation,
        HitByEnemyReaction hitExecuter,
        HitByEnemyPredicate hitDeterminer)
    {
        var job = GetCurrentJob();
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        new Collider(go, shape, Collision.Player, BodyType.Dynamic, true);
        job.TrackResource(go);
        return new EnemyAttack(go, AttackType.Blade, job, hitExecuter, hitDeterminer);
    }
    
    public static class Animations
    {
        public static LineTraceAnimation LineTrace(
            Vector2 point1,
            Vector2 point2,
            float thickness,
            float duration,
            Color startColor,
            float layerDepth = 0)
        {
            var trace = new LineTraceAnimation(
                point1,
                point2,
                thickness,
                duration,
                startColor,
                layerDepth);
            AnimationsPool.Add(trace);
            return trace;
        }

        public static CircleIdleAnimation CircleIdle(
            Vector2 position,
            float radius,
            Color color,
            float layerDepth = 0)
        {
            var job = GetCurrentJob();
            var animation = new CircleIdleAnimation( radius, color, layerDepth);
            animation.Transform.Position = position;
            AnimationsPool.Add(animation);
            job.TrackResource(animation);
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

        public static Shape PolygonShape(Vertices vertices)
        {
            return new PolygonShape(vertices, 1.0f);
        }
        
        public static Shape PolygonShape(Vector2[] vertices)
        {
            var nkastVertices = new Vertices(vertices);
            return new PolygonShape(nkastVertices, 1.0f);
        }
    }
}