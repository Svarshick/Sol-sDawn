using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic;

public static class Debug
{
    public static void DrawDot(Vector2 position, Color color, float duration = 5f)
    {
            Game.AnimationsPool.Add(new CircleTrace(
                new Transform { Position = position },
                20,
                20,
                20,
                duration,
                color,
                Color.Transparent));
    }
}