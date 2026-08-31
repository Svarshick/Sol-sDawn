using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    
    #region Utils
    
    private static Job GetCurrentJob() => JobContext.CurrentJob ?? throw new NullReferenceException("Current Job is null");

    private static GameObject CreateGameObject(Job job, Vector2 position, float rotation = 0)
    {
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        job.TrackResource(go);
        return go;
    }

    #endregion

    public static GameObject CreateObject(Vector2 position = default, float rotation = 0)
    {
        var job = GetCurrentJob();
        var go = CreateGameObject(job, position, rotation);
        return go;
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

    public static Vector2 Rotated(this Vector2 v, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        return new Vector2(
            v.X * cos - v.Y * sin,
            v.X * sin + v.Y * cos
        );
    }

    public static float PI => MathF.PI;
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

    public static class Fight
    {
        public struct FireParryCastResult
        {
            public FireParryWindow ParryWindow;
            public Vector2 Position;
        }

        public static bool FireParryCast(
            Vector2 start,
            Vector2 end,
            float width,
            out FireParryCastResult result)
        {
            result = default;
            if (!Collision.LineCast(start, end, width, Collision.FireParry, out var hit))
                return false;

            if (hit.Fixture.Body.Tag is not Collider collider ||
                !collider.GameObject.TryGetComponent<FireParryWindow>(out var parryWindow))
                throw new LogicException();

            result.Position = hit.HitPoint;
            result.ParryWindow = parryWindow;
            return true;
        }
        
        public static Attack PlayerBladeAttack(
            Shape shape,
            Vector2 position,
            float rotation,
            HitPredicate? hitDeterminer,
            HitReaction? hitExecuter)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new Attack(go, job, shape, Collision.BladeAttack, Collision.Enemy, hitDeterminer, hitExecuter);
        }
        
        public static PlayerBladeParryingAttack PlayerBladeParryingAttack(
            Shape shape,
            Vector2 position,
            float rotation,
            HitPredicate? hitDeterminer,
            HitReaction? hitExecuter,
            BladeParryReaction? parryReaction)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new PlayerBladeParryingAttack(go, job, shape, hitDeterminer, hitExecuter, parryReaction);
        }
        
        public static Attack PlayerFireAttack(
            Shape shape,
            Vector2 position,
            float rotation,
            HitPredicate? hitDeterminer,
            HitReaction? hitExecuter)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new Attack(go, job, shape, Collision.FireAttack, Collision.Enemy, hitDeterminer, hitExecuter);
        }

        public static Attack EnemyBladeAttack(
            Shape shape,
            Vector2 position,
            float rotation,
            HitPredicate? hitDeterminer,
            HitReaction? hitExecuter)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new Attack(go, job, shape, Collision.BladeAttack, Collision.Player, hitDeterminer, hitExecuter);
        }
       
        public static Attack EnemyFireAttack(
            Shape shape,
            Vector2 position,
            float rotation,
            HitPredicate? hitDeterminer,
            HitReaction? hitExecuter)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new Attack(go, job, shape, Collision.FireAttack, Collision.Player, hitDeterminer, hitExecuter);
        }

        public static BladeParryWindow BladeParryWindow(
            Entity? owner,
            Shape shape,
            Vector2 position,
            float rotation,
            BladeParryReaction parriedReaction,
            BladeParryPredicate? parryDeterminer = null)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new BladeParryWindow(go, owner, job, shape, parriedReaction, parryDeterminer);
        }

        public static FireParryWindow FireParryWindow(
            Entity? owner,
            Shape shape,
            Vector2 position,
            float rotation,
            FireParryReaction parryBumpReaction,
            FireParryReaction parriedReaction,
            FireParryPredicate? parryDeterminer = null)
        {
            var job = GetCurrentJob();
            var go = CreateGameObject(job, position, rotation);
            return new FireParryWindow(go, owner, job, shape, parriedReaction, parryBumpReaction, parryDeterminer);
        }
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