using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Collision.Shapes;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Animations;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;
using SolsDawn.Core.Logic.Gameplay.Lua;

namespace SolsDawn.Core.Logic;

public sealed class Game : Microsoft.Xna.Framework.Game
{
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static AnimationsPool AnimationsPool { get; private set; }
    public static CartesianCamera Camera { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }

    private GraphicsDeviceManager _graphicsDeviceManager;
    private Player _player;
    private Input _input;
    private PlayerController _playerController;
    private GameTests _gameTests;
    private LuaMain _luaMain; 

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
        var playerShape = new CircleShape(100, 1);
        new Collider(playerGo, playerShape, Collision.Player);
        new HP(playerGo, 10);
        new Animator<PlayerAnimations>(playerGo, new PlayerAnimations());
        _player = new Player(playerGo);
        new HUD(playerGo, _player);
        
        var bossGo = new GameObject();
        var bossShape = new CircleShape(100, 1);
        new Collider(bossGo, bossShape, Collision.Enemy);
        new HP(bossGo, 10);
        new Animator<BossAnimations>(bossGo, new BossAnimations());
        var boss = new Entity(bossGo);

        IntentionsPool.Blackboard = new FightBlackboard(boss, _player, Camera);

        _playerController = new(_player, _input);
        _gameTests = new();
        return;

        void InitSystems()
        {
            AnimationsPool = new AnimationsPool();
            _input = new Input();
            _luaMain = new LuaMain("Configs/Lua", _input);
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
        
        AnimationsPool.Update();
        Camera.Position = _player.GameObject.Transform.Position;

        _luaMain.Update();
        
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
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);
        
        SpriteBatch.Begin(
            sortMode: SpriteSortMode.FrontToBack,
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: Camera.GetViewMatrix()
        );
        
        SpriteBatch.DrawCircle(Vector2.Zero, 0.2f, 10, Color.Azure, 0.2f);
        
        AnimationsPool.Draw();
        _gameTests.Draw();
        GameObjectPool.Draw();
        
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}