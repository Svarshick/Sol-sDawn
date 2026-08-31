using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SolsDawn.Core.Logic;

public class BoardBackground
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _whitePixel;
    private readonly Effect _effect;

    public float CellSize { get; set; } = 1.0f;
    public Color ColorA { get; set; } = new Color(35, 35, 35);
    public Color ColorB { get; set; } = new Color(45, 45, 45);

    public BoardBackground(GraphicsDevice graphicsDevice, Game game)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _effect = game.Content.Load<Effect>("Content/Board"); 
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(CartesianCamera camera)
    {
        var viewport = _graphicsDevice.Viewport;

        _effect.Parameters["CameraPosition"]?.SetValue(camera.Position);
        _effect.Parameters["Zoom"]?.SetValue(camera.Zoom);
        _effect.Parameters["PPU"]?.SetValue((float)camera.PPU);
        _effect.Parameters["ViewportSize"]?.SetValue(new Vector2(viewport.Width, viewport.Height));
        _effect.Parameters["CellSize"]?.SetValue(CellSize);
        _effect.Parameters["ColorA"]?.SetValue(ColorA.ToVector4());
        _effect.Parameters["ColorB"]?.SetValue(ColorB.ToVector4());

        _spriteBatch.Begin(effect: _effect);
        _spriteBatch.Draw(_whitePixel, viewport.Bounds, Color.White);
        _spriteBatch.End();
    }
}