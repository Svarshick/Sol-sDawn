using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SolsDawn.Core.Effects;
using SolsDawn.Core.Gameplay;

namespace SolsDawn.Core;

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
    private Boss _boss;
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

        _boss = new Boss(_spriteBatch, _screenLayout);
        _player = new Player(_spriteBatch, _effectsPool, _screenLayout, _input);
        _updatables.Add(_player);
        _updatables.Add(_boss);
        _drawables.Add(_boss);
        _drawables.Add(_player);
        return;

        void InitSystems()
        {
            _input = new Input();
            _effectsPool = new EffectsPool();
        }

        void InitScreen()
        {
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
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.MonoGameOrange);

        _spriteBatch.Begin(
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: _screenLayout.Camera.GetViewMatrix()
        );

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