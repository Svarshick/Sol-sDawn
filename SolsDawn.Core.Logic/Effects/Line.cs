using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Effects;

public class Line : IPassiveEffect
{
    public Vector2 Start;
    public Vector2 End;
    public Color Color;
    public float Thickness;
    public float LayerDepth;

    public Line(
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness,
        float layerDepth = 0.0f)
    {
        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
        LayerDepth = layerDepth;
    }

    public bool IsFinished { get; set; }

    public void Draw(GameTime gameTime)
    {
        Game.SpriteBatch.DrawLine(
            Start,
            End,
            Color,
            Thickness,
            LayerDepth
        );
    }
}