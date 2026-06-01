using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public class LineIdle(
    Transform transform,
    Vector2 end,
    float thickness,
    Color color,
    float layerDepth = 0.0f)
    : IAnimation
{
    public Vector2 End = end;
    public Color Color = color;
    public float Thickness = thickness;
    public float LayerDepth = layerDepth;

    public bool IsFinished { get; set; } = false;

    public void Draw()
    {
        Game.SpriteBatch.DrawLine(
            transform.Position,
            End,
            Color,
            Thickness,
            LayerDepth
        );
    }

    public void Cancel() => IsFinished = true;
}