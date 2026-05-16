using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Effects;

public class Line : IPassiveEffect
{
    public Vector2 Start;
    public Vector2 End;
    public Color Color;
    public float Thickness;

    private SpriteBatch _spriteBatch;

    public Line(
        SpriteBatch spriteBatch,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        _spriteBatch = spriteBatch;

        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
    }

    public bool IsFinished { get; set; }

    public void Draw(GameTime gameTime)
    {
        _spriteBatch.DrawLine(
            Start,
            End,
            Color,
            Thickness
        );
    }
}