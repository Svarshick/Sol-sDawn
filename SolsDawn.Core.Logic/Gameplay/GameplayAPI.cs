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
    #region Private

    private static GameObject CreateGameObject(Job job, Vector2 position, float rotation = 0)
    {
        var go = new GameObject();
        go.Transform.Position = position;
        go.Transform.Rotation = rotation;
        job.TrackResource(go);
        return go;
    }

    #endregion

    #region Meta
    
    public static CartesianCamera Camera { get; internal set; }
    public static Painter Painter { get; internal set; }
    public static Input Input { get; internal set; }
    public static AnimationsPool AnimationsPool { get; internal set; }
    public static float DeltaTime => (float)Time.ElapsedGameTime.TotalSeconds;
    public static float TotalTime => (float)Time.TotalGameTime.TotalSeconds;
    public static Action ImGuiDrawer
    {
        get => Game.ImGuiDrawer;
        set => Game.ImGuiDrawer = value;
    }

    public static readonly float FpsSec = 1/60f;
    
    #endregion
    
    #region Math
    
    public static float Abs(float f) => Math.Abs(f);

    public static int Sign(float f) => Math.Sign(f);
    
    public static float Max(float f1, float f2) => Math.Max(f1, f2);
    
    public static float Min(float f1, float f2) => Math.Min(f1, f2);
    
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

    public static float Tan(float radians) => (float)Math.Tan(radians);

    public static float Sin(float radians) => (float)Math.Sin(radians);

    public static float Cos(float radians) => (float)Math.Cos(radians);
    
    #endregion
    
    #region Job
    
    public static YieldAwaiter NextFrame() => new(CurrentJob);

    public static Timer Timer(double delay)
    {
        var job = CurrentJob;
        var timer = new Timer(job, delay);
        job.StartTimer(timer);
        return timer;
    }

    public static Event Event()
    {
        var job = CurrentJob;
        var @event = new Event(job);
        job.TrackResource(@event);
        return @event;
    }
    
    public static EventRace Race(params object[] args)
    {
        var job = CurrentJob;
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
    
    #endregion

    public static Job CurrentJob => JobContext.CurrentJob ?? throw new NullReferenceException("Current Job is null");
    
    public static class Layer
    {
        public static Category Wall => Category.Cat1;
        public static Category Player => Category.Cat2;
        public static Category Enemy => Category.Cat3;
    }

    public static GameObject CreateObject(Vector2 position = default, float rotation = 0)
    {
        var job = CurrentJob;
        var go = CreateGameObject(job, position, rotation);
        return go;
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
            var job = CurrentJob;
            var animation = new CircleIdleAnimation( radius, color, layerDepth);
            animation.Transform.Position = position;
            AnimationsPool.Add(animation);
            job.TrackResource(animation);
            return animation;
        }
    }

    public static class Shapes
    {
        public static CircleShape Circle(float radius)
        {
            return new CircleShape(radius, 1.0f);
        }

        public static PolygonShape Rectangle(float width, float height)
        {
            var vertices = PolygonTools.CreateRectangle(width / 2, height / 2);
            return new PolygonShape(vertices, 1.0f);
        }

        public static PolygonShape Square(float side) => Rectangle(side, side);

        public static PolygonShape PolygonShape(Vertices vertices)
        {
            return new PolygonShape(vertices, 1.0f);
        }
        
        public static PolygonShape PolygonShape(Vector2[] vertices)
        {
            var nkastVertices = new Vertices(vertices);
            return new PolygonShape(nkastVertices, 1.0f);
        }

        public static EdgeShape EdgeShape(float width)
        {
            return new EdgeShape(new Vector2(-width / 2, 0), new Vector2(width / 2, 0));
        }
    }
}