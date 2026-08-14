using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

public static class Helper
{
    public static Vector2[] ArrowPentagonVertices(
        Vector2 from,
        Vector2 direction,
        float tailLength,
        float tailWidth,
        float headLength,
        float headWidth)
    {
        var perp = new Vector2(-direction.Y, direction.X);
        return
        [
            from + perp * (tailWidth/2),
            from + direction * tailLength + perp * (headWidth/2),
            from + direction * (tailLength + headLength),
            from + direction * tailLength - perp * (headWidth/2),
            from - perp * (tailWidth/2),
        ];
    }
}