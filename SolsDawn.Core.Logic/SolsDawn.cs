using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Collision.Shapes;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic;

public sealed class SolsDawn : Game
{
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static AnimationsPool AnimationsPool { get; private set; }
    public static CartesianCamera Camera { get; private set; }
    public static Painter Painter { get; private set; }

    private GraphicsDeviceManager _graphicsDeviceManager;
    private Input _input;
    private PlayerController _playerController;
    private GameTests _gameTests;

    public SolsDawn()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitScreen();
        Painter = new Painter(GraphicsDevice);
        InitSystems();

        var playerGo = new GameObject();
        var playerShape = new CircleShape(1, 1);
        new Collider(playerGo, playerShape, Collision.Player);
        new HP(playerGo, 10);
        new Animator<PlayerAnimations>(playerGo, new PlayerAnimations());
        var player = new Player(playerGo);
        new HUD(playerGo, player);
        
        IntentionsPool.Blackboard = new FightBlackboard(player, Camera);

        BehaviourController.Player = player;
        _playerController = new(player, _input);
        _gameTests = new(this);
        _gameTests.IsActive = false;
        return;

        void InitSystems()
        {
            AnimationsPool = new AnimationsPool();
            _input = new Input();
        }
        
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
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        MonoTask.Update();
        Collision.Update(gameTime);
        
        _input.Update();
        _playerController.Update();
        IntentionsPool.Resolve();
        AffectsPool.Resolve();
        BehaviourController.Update();
        GameObjectPool.Update();
        
        AnimationsPool.Update();
        
        Camera.Position = BehaviourController.Player.GameObject.Transform.Position;
        
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