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
    public static Action? ImGuiDrawer { get; set; }
    
    private GraphicsDeviceManager _graphicsDeviceManager;
    private ImGuiRenderer _imGuiRenderer;
    private BoardBackground _background;
    
    private Func<Job> _mainJobRunner;
    private Job _mainJob;
    private bool _firstUpdate = true;
    private Input _input;
    private string _baseTitle = string.Empty;
    
    public Game(Func<Job> mainJobRunner)
    {
        IsFixedTimeStep = true;
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        _mainJobRunner = mainJobRunner;
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitScreen();
        InitSystems();
        return;

        void InitScreen()
        {
            Camera = new CartesianCamera(GraphicsDevice);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            _baseTitle = Window.Title;
            Camera.Position = Vector2.Zero;
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphicsDeviceManager.PreferredBackBufferWidth = displayMode.Width;
            _graphicsDeviceManager.PreferredBackBufferHeight = displayMode.Height;
            _graphicsDeviceManager.IsFullScreen = false; 
            _graphicsDeviceManager.ApplyChanges();
            _background = new BoardBackground(GraphicsDevice, this);
        }
        
        void InitSystems()
        {
            
            _input = new Input();
            Painter = new Painter(GraphicsDevice);
            AnimationsPool = new AnimationsPool();
            _imGuiRenderer = new(this);
            _imGuiRenderer.RebuildFontAtlas();

            GameplayAPI.Init(this, Camera, Painter, _input, AnimationsPool);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        
        _input.Update();
        SaveSystem.Update();
        if (_firstUpdate)
        {
            _mainJob = _mainJobRunner();
            _firstUpdate = false;
        }
        else
        {
            _mainJob.Update();
        }

        GameObjectPool.Update();
        AnimationsPool.Update();
        
        base.Update(gameTime);
        LateUpdate();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var title = SaveSystem.HasUnsavedChanges ? _baseTitle + " *" : _baseTitle;
        if (Window.Title != title)
            Window.Title = title;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (SaveSystem.CancelExit())
        {
            args.Cancel = true;
            return;
        }

        base.OnExiting(sender, args);
    }

    private void LateUpdate()
    {
        _input.LateUpdate();
        AnimationsPool.LateUpdate();

        GameObjectPool.LateUpdate();
    }

    protected override void Draw(GameTime gameTime)
    {
        _background.Draw(Camera);

        AnimationsPool.Draw();
        GameObjectPool.Draw();

        Painter.Begin(
            view: Camera.CreateViewMatrix(),
            rasterizerState: RasterizerState.CullClockwise
        );
        Painter.DoDraws();
        Painter.End();

        if (ImGuiDrawer is not null || SaveSystem.ExitPromptOpen)
        {
            _imGuiRenderer.BeforeLayout(gameTime);
            ImGuiDrawer?.Invoke();
            SaveSystem.DrawExitPrompt(Exit);
            _imGuiRenderer.AfterLayout();
        }

        base.Draw(gameTime);
    }
}