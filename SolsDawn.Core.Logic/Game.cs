using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic;

public sealed class Game : Microsoft.Xna.Framework.Game
{
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static AnimationsPool AnimationsPool { get; private set; }
    public static ScreenLayout ScreenLayout { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public static LuaMain LuaMain { get; private set; } = new();

    private GraphicsDeviceManager _graphicsDeviceManager;
    private Player _player;
    private Input _input;
    private PlayerController _playerController;

    private GameTests _gameTests;

    public Game()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitScreen();
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        InitSystems();

        var playerGo = new GameObject();
        new Collider(playerGo, 1, Collision.LayerName.Player);
        new Hp(playerGo, 10);
        new Animator<PlayerAnimations>(playerGo, new PlayerAnimations());
        _player = new Player(playerGo);
        new HUD(playerGo, _player);
        
        var bossGo = new GameObject();
        new Collider(bossGo, 2, Collision.LayerName.Enemy);
        new Hp(bossGo, 10);
        new Animator<BossAnimations>(bossGo, new BossAnimations());
        var boss = new Boss(bossGo);

        IntentionsPool.Blackboard = new FightBlackboard(boss, _player, ScreenLayout);

        _playerController = new(_player, _input);
        _gameTests = new();
        return;

        void InitSystems()
        {
            AnimationsPool = new AnimationsPool();
            _input = new Input();
        }
        
        void InitScreen()
        {
            IsMouseVisible = false;
            ScreenLayout = new ScreenLayout(Window, GraphicsDevice);
            _graphicsDeviceManager.PreferredBackBufferWidth = ScreenLayout.WidthResolution;
            _graphicsDeviceManager.PreferredBackBufferHeight = ScreenLayout.HeightResolution;
            _graphicsDeviceManager.HardwareModeSwitch = false;
            _graphicsDeviceManager.IsFullScreen = true;
            _graphicsDeviceManager.ApplyChanges();
            Window.AllowUserResizing = false;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        MonoTask.Update();
        
        AnimationsPool.Update();
        ScreenLayout.FollowPosition(_player.GameObject.Transform.Position);

        LuaMain.Update();
        
        _gameTests.Update();
        _input.Update();
        _playerController.Update();
        GameObjectPool.Update();
        IntentionsPool.Resolve();
        AffectsPool.Resolve();
        
        base.Update(gameTime);
        LateUpdate();
    }

    private void LateUpdate()
    {
        _input.LateUpdate();
        AnimationsPool.LateUpdate();

        _gameTests.LateUpdate();
        GameObjectPool.LateUpdate();
        Collision.World.RebuildDynamicLayers();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);
        
        SpriteBatch.Begin(
            sortMode: SpriteSortMode.FrontToBack,
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: ScreenLayout.Camera.GetViewMatrix()
        );
        
        SpriteBatch.DrawCircle(Vector2.Zero, 20f, 10, Color.Azure, 20f);
        
        AnimationsPool.Draw();
        _gameTests.Draw();
        GameObjectPool.Draw();
        
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}