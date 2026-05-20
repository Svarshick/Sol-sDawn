using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Effects;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Animations;

namespace SolsDawn.Core.Logic;

public sealed class Game : Microsoft.Xna.Framework.Game
{
    private ScreenLayout _screenLayout;

    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    private GraphicsDeviceManager _graphicsDeviceManager;
    private SpriteBatch _spriteBatch;

    private Input _input;
    private EffectsPool _effectsPool;

    private Player _player;
    private BossAI _bossAI;

    public Game()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitScreen();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        InitSystems();

        var playerGo = new GameObject();
        new Collider(playerGo, 1, Collision.LayerName.Player);
        _player = new Player(playerGo, _spriteBatch, _effectsPool, _screenLayout, _input);
        var bossGo = new GameObject();
        new Collider(bossGo, 2, Collision.LayerName.Enemy);
        new Hp(bossGo, 10);
        new Animator(bossGo, new BossAnimations(_spriteBatch, _screenLayout));
        var boss = new Boss(bossGo, _spriteBatch, _effectsPool, _screenLayout);

        _bossAI = new(boss);
        return;

        void InitSystems()
        {
            _input = new Input();
            _effectsPool = new EffectsPool();
        }

        void InitScreen()
        {
            IsMouseVisible = false;
            _screenLayout = new ScreenLayout(Window, GraphicsDevice);
            _graphicsDeviceManager.PreferredBackBufferWidth = _screenLayout.WidthResolution;
            _graphicsDeviceManager.PreferredBackBufferHeight = _screenLayout.HeightResolution;
            _graphicsDeviceManager.HardwareModeSwitch = false;
            _graphicsDeviceManager.IsFullScreen = true;
            _graphicsDeviceManager.ApplyChanges();
            Window.AllowUserResizing = false;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        MonoTask.Update(gameTime);
        
        _effectsPool.Update(gameTime);
        _screenLayout.FollowPosition(_player.GameObject.Position);
        
        _input.Update(gameTime);
        _bossAI.Update(gameTime);
        GameObjectPool.Update(gameTime);
        AffectResolver.Resolve();
        
        base.Update(gameTime);
        LateUpdate(gameTime);
    }

    private void LateUpdate(GameTime gameTime)
    {
        _input.LateUpdate(gameTime);
        _effectsPool.LateUpdate(gameTime);

        GameObjectPool.LateUpdate(gameTime);
        Collision.World.RebuildDynamicLayers();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);
        

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.FrontToBack,
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: _screenLayout.Camera.GetViewMatrix()
        );
        
        _effectsPool.Draw(gameTime);
        GameObjectPool.Draw(gameTime);
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}