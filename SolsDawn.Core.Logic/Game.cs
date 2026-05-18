using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Effects;
using SolsDawn.Core.Logic.Gameplay;

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

    private List<IDrawable> _drawables;
    private List<IUpdatable> _updatables;
    private Player _player;

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

        _updatables = new();
        _drawables = new();

        var go = new GameObject();
        new Collider(go, 1, Collision.LayerName.Player);
        _player = new Player(go, _spriteBatch, _effectsPool, _screenLayout, _input);
        _updatables.Add(go);
        _drawables.Add(go);
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

        _input.Update(gameTime);
        _effectsPool.Update(gameTime);

        _screenLayout.FollowPosition(_player.Position);

        foreach (var updatable in _updatables)
            updatable.Update(gameTime);
        
        base.Update(gameTime);
        LateUpdate(gameTime);
    }

    private void LateUpdate(GameTime gameTime)
    {
        _input.LateUpdate(gameTime);
        _effectsPool.LateUpdate(gameTime);

        foreach (var updatable in _updatables)
            updatable.LateUpdate(gameTime);
        
        Collision.World.RebuildDynamicLayers();
        var circle = new BoundingCircle2D(_c, _r);
        var bounds = BoundingBox2D.CreateFromCenterAndExtents(circle.Center, new(circle.Radius));
        _player.Stats.Color = MainConfig.PlayerStats.Color;
        foreach (var actor in Collision.World.QueryCandidates(bounds, Collision.LayerName.Player))
        {
            var shape = new CollisionShape2D(circle);
            if (actor.Shape.Intersects(shape))
            {
                _player.Stats.Color = Color.Red;
            }
        }
    }

    private Vector2 _c = new Vector2(15, 15);
    private float _r = 15f;
    private Vector2 _b = Vector2.Zero;
    private Vector2 _e = new Vector2(30, 30);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.MonoGameOrange);

        _spriteBatch.Begin(
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: _screenLayout.Camera.GetViewMatrix()
        );
        
        _spriteBatch.DrawRectangle(_b, _e, Color.Aqua, 30);

        _effectsPool.Draw(gameTime);

        _spriteBatch.DrawLine(0, 0, 1000, 0, Color.White, 3);
        _spriteBatch.DrawLine(0, 0, -1000, 0, Color.Black, 3);
        _spriteBatch.DrawLine(0, 0, 0, -1000, Color.White, 3);
        _spriteBatch.DrawLine(0, 0, 0, 1000, Color.Black, 3);

        foreach (var drawable in _drawables)
            drawable.Draw(gameTime);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}