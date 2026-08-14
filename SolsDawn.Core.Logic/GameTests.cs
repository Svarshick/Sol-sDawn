using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic;

public class GameTests : IUpdatable, IDrawable
{
    public bool IsActive = false;
    private readonly SolsDawn _game;
    private readonly ShapeBatch _shapeBatch;
    private readonly SpriteBatch _spriteBatch;

    public GameTests(SolsDawn game)
    {
        _game = game;
        _shapeBatch = new(game.GraphicsDevice, game.Content);
        _spriteBatch = new (game.GraphicsDevice);
    }

    public void Update()
    {
         if (!IsActive)
            return;
    }

    public void LateUpdate()
    {
        if (!IsActive)
            return;
    }

    public void Draw()
    {
        if (!IsActive)
            return;

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.FrontToBack,
            rasterizerState: RasterizerState.CullNone,
            transformMatrix: SolsDawn.Camera.CreateViewMatrix());
        
        _shapeBatch.Begin(
            view: SolsDawn.Camera.CreateViewMatrix(),
            rasterizerState: RasterizerState.CullClockwise
        );
        
        _shapeBatch.FillRectangle(new Vector2(0, 0), new Vector2(3, 1), Color.Red);
        _shapeBatch.FillRectangle(new Vector2(0, 0), new Vector2(3, 1), Color.Blue, 0, 1);
        _spriteBatch.DrawCircle(new Vector2(0, 0), 2, 20, Color.Green, 0.2f);
        _shapeBatch.FillCircle(new Vector2(0, 0), 0.2f, Color.Yellow);
        
        _shapeBatch.End();
        _spriteBatch.End();
    }
}