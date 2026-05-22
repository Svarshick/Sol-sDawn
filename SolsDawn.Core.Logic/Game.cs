using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Effects;
using SolsDawn.Core.Logic.Gameplay;
using SolsDawn.Core.Logic.Gameplay.Animations;
using SolsDawn.Core.Logic.Gameplay.Interaction;

namespace SolsDawn.Core.Logic;

public sealed class Game : Microsoft.Xna.Framework.Game
{
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public readonly static bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static EffectsPool EffectsPool { get; private set; }
    public static ScreenLayout ScreenLayout { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }

    private GraphicsDeviceManager _graphicsDeviceManager;
    private Player _player;
    private Input _input;
    private BossAI _bossAI;

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
        new Animator(playerGo, new PlayerAnimations(), PlayerAnimations.Idle);
        _player = new Player(playerGo, _input);
        new HUD(playerGo, _player);
        
        var bossGo = new GameObject();
        new Collider(bossGo, 2, Collision.LayerName.Enemy);
        new Hp(bossGo, 10);
        new Animator(bossGo, new BossAnimations(), BossAnimations.Idle);
        var boss = new Boss(bossGo);

        IntentionsPool.PlayerGO = playerGo;
        IntentionsPool.BossGO = bossGo;
        var context = new BossBehaviourContext(boss, _player, ScreenLayout);
        _bossAI = new(MainConfig.BossBehaviourBuilder, context);
        return;

        void InitSystems()
        {
            EffectsPool = new EffectsPool();
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
        MonoTask.Update(gameTime);
        
        EffectsPool.Update(gameTime);
        ScreenLayout.FollowPosition(_player.GameObject.Transform.Position);
        
        _input.Update(gameTime);
        _bossAI.Update(gameTime);
        GameObjectPool.Update(gameTime);
        IntentionsPool.Resolve();
        AffectsPool.Resolve();
        
        base.Update(gameTime);
        LateUpdate(gameTime);
    }

    private void LateUpdate(GameTime gameTime)
    {
        _input.LateUpdate(gameTime);
        EffectsPool.LateUpdate(gameTime);

        GameObjectPool.LateUpdate(gameTime);
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
        
        EffectsPool.Draw(gameTime);
        GameObjectPool.Draw(gameTime);
        
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}