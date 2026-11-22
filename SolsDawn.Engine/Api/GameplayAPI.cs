using System;
using System.Collections.Generic;
using Path = System.IO.Path;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace SolsDawn.Engine;

public static class GameplayAPI
{
    internal static void Init(
        Game game,
        CartesianCamera camera,
        Painter painter,
        Input input,
        AnimationsPool animationsPool)
    {
        Game = game;
        Camera = camera;
        Painter = painter;
        Input = input;
        AnimationsPool = animationsPool;
    }

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

    #region Engine

    private static Game Game;
    internal static CartesianCamera Camera { get; private set; }
    internal static Painter Painter { get; private set; }
    internal static Input Input { get; private set; }
    internal static AnimationsPool AnimationsPool { get; private set; }

    internal static Action? ImGuiDrawer
    {
        get => Game.ImGuiDrawer;
        set => Game.ImGuiDrawer = value;
    }

    internal static bool WantCaptureKeyboard =>
        Game.ImGuiDrawer is not null &&
        ImGui.GetCurrentContext() != IntPtr.Zero &&
        ImGui.GetIO().WantCaptureKeyboard;

    internal static bool WantCaptureMouse =>
        Game.ImGuiDrawer is not null &&
        ImGui.GetCurrentContext() != IntPtr.Zero &&
        ImGui.GetIO().WantCaptureMouse;

    internal static readonly float FpsSec = 1 / 60f;

    internal static Job CurrentJob => JobContext.CurrentJob ?? throw new NullReferenceException("Current Job is null");

    internal static class Layer
    {
        public static Category None => Category.None;
        public static Category Wall => Category.Cat1;
        public static Category Player => Category.Cat2;
        public static Category Enemy => Category.Cat3;
    }

    #endregion

    #region Startup

    public static string DataDirectory(string subdirectory = "", [CallerFilePath] string callerFile = "")
    {
#if DEBUG
        var root = Path.Combine(Path.GetDirectoryName(callerFile)!, "_Data");
#else
        var root = Path.Combine(AppContext.BaseDirectory, "_Data");
#endif
        return Path.Combine(root, subdirectory);
    }

    public static void InitializeLibrary(string directory) => Library.Initialize(directory);

    public static T LoadRoot<T>() where T : class, new() => Library.LoadRoot<T>();

    public static void InitializeSpeedGraphs(string directory) => SpeedGraphs.Initialize(directory);

    public static Job RunLevels(JobMethod start) => LevelRunner.Run(start);

    #endregion

    #region Time

    public static float DeltaTime => (float)Time.ElapsedGameTime.TotalSeconds;
    public static float TotalTime => (float)Time.TotalGameTime.TotalSeconds;
    public static long TotalFrames => Time.TotalGameTime.Ticks / Game.TargetElapsedTime.Ticks;

    #endregion

    #region Math

    public const float PI = MathF.PI;

    public static float Abs(float f) => Math.Abs(f);

    public static float Sign(float f) => MathF.Sign(f);

    public static float Max(float f1, float f2) => Math.Max(f1, f2);

    public static float Min(float f1, float f2) => Math.Min(f1, f2);

    public static float Clamp(float f, float min, float max) => Math.Clamp(f, min, max);

    public static float Lerp(float from, float to, float t) => from + (to - from) * t;

    public static float Sqrt(float f) => MathF.Sqrt(f);

    public static float Pow(float f, float power) => MathF.Pow(f, power);

    public static float Exp(float f) => MathF.Exp(f);

    public static float Floor(float f) => MathF.Floor(f);

    public static float Round(float f) => MathF.Round(f);

    public static float Tan(float radians) => (float)Math.Tan(radians);

    public static float Sin(float radians) => (float)Math.Sin(radians);

    public static float Cos(float radians) => (float)Math.Cos(radians);

    public static float ASin(float f) => (float)Math.Asin(f);

    public static float ACos(float f) => (float)Math.Acos(f);

    public static float ATan2(float y, float x) => MathF.Atan2(y, x);

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

    public static Vector2 Direction(Vector2 from, Vector2 to) => CombatMath.Direction(from, to);

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

    #region Objects

    public static GameObject CreateObject(Vector2 position = default, float rotation = 0)
    {
        var job = CurrentJob;
        var go = CreateGameObject(job, position, rotation);
        return go;
    }

    public static class G
    {
        public static Player? Player { get; internal set; }
    }

    public static class Spawn
    {
        public static Platform Platform(Vector2 position, float width, float rotation = 0) =>
            new(CreateObject(position, rotation), width);

        public static Player Player(Vector2 position, CategorySlot<PlayerSpecs> specs)
        {
            if (G.Player is { IsDestroyed: false })
                Log.Logic.Fatal($"Level {LevelRunner.CurrentName} spawned a second player.");

            var board = new PlayerBoard { Specs = specs };
            var player = new Player(CreateObject(position), board, new PlayerAnimations(board));
            G.Player = player;
            return player;
        }

        public static Enemy Enemy(Vector2 position, CategorySlot<EnemySpecs> specs, Func<Enemy, Job> behaviour)
        {
            var board = new EnemyBoard { Specs = specs };
            var enemy = new Enemy(CreateObject(position), board, new EnemyAnimations(board));
            enemy.Behaviour = behaviour(enemy);
            return enemy;
        }
    }

    #endregion

    #region Combat

    public static Attack CreateAttack(GameObject go, Shape shape, Side targets, ParryKind parryKind) =>
        new(go, shape, targets, parryKind);

    public static Parrybox CreateParrybox(GameObject go, Shape shape, Side side, ParryKind accepts) =>
        new(go, shape, side, accepts);

    #endregion

    #region Animations

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
            var animation = new CircleIdleAnimation(radius, color, layerDepth);
            animation.Transform.Position = position;
            AnimationsPool.Add(animation);
            job.TrackResource(animation);
            return animation;
        }
    }

    #endregion

    #region Shapes

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

        internal static PolygonShape PolygonShape(Vertices vertices)
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

        public static Shape EnemyBody(EnemySpecs specs) => Enemy.BodyShape(specs);
    }

    #endregion
}
