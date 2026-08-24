using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic;

public sealed class Game : Microsoft.Xna.Framework.Game
{
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static CartesianCamera Camera { get; private set; }
    public static Painter Painter { get; private set; }
    public static AnimationsPool AnimationsPool { get; private set; }
    public static IntentionsPool IntentionsPool { get; private set; }
    
    private GraphicsDeviceManager _graphicsDeviceManager;
    private Func<Job> _mainJobRunner;
    private Job _mainJob;
    private Input _input;
    
    private GameTests _gameTests;

    public Game(Func<Job> mainJobRunner)
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        _mainJobRunner = mainJobRunner;
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitScreen();
        Painter = new Painter(GraphicsDevice);
        InitSystems();

        _gameTests = new(this);
        _gameTests.IsActive = false;

        return;

        void InitScreen()
        {
            Camera = new CartesianCamera(GraphicsDevice);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Camera.Position = Vector2.Zero;
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphicsDeviceManager.PreferredBackBufferWidth = displayMode.Width;
            _graphicsDeviceManager.PreferredBackBufferHeight = displayMode.Height;
            _graphicsDeviceManager.IsFullScreen = false; 
            _graphicsDeviceManager.ApplyChanges();
        }
        
        void InitSystems()
        {
            _input = new Input();
            AnimationsPool = new AnimationsPool();
            IntentionsPool = new  IntentionsPool();

            GameplayAPI.Camera = Camera;
            GameplayAPI.Painter = Painter;
            GameplayAPI.Input = _input;
            GameplayAPI.AnimationsPool = AnimationsPool;
            GameplayAPI.IntentionsPool = IntentionsPool;
            _mainJob = _mainJobRunner();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        MonoTask.Update();
        _input.Update();
        
        Collision.Update(gameTime);
        _mainJob.Update();
        GameObjectPool.Update();
        AnimationsPool.Update();
        
        _gameTests.Update();
        
        base.Update(gameTime);
        LateUpdate();
    }

    private void LateUpdate()
    {
        _input.LateUpdate();
        AnimationsPool.LateUpdate();

        _gameTests.LateUpdate();
        GameObjectPool.LateUpdate();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);
        
        AnimationsPool.Draw();
        _gameTests.Draw();
        GameObjectPool.Draw();
        
        Painter.Begin(
            view: Camera.CreateViewMatrix(),
            rasterizerState: RasterizerState.CullClockwise
        );
        Painter.DoDraws();
        Painter.End();
        base.Draw(gameTime);
    }
}