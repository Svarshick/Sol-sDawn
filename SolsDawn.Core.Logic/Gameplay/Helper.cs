using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Helper
{
    public static Vector2[] ArchVertices(
        Vector2 from,
        Vector2 direction,
        float length,
        float width,
        float angle,
        float edgeLength)
    {
        var tip = from + direction * length;
        var edgeLine = new Vector2(0, -edgeLength);
        return
        [
            from + direction.PerpendicularCounterClockwise() * width / 2,
            tip + Vector2.Rotate(edgeLine, MathHelper.Pi - angle / 2 + direction.ToAngle()),
            tip,
            tip + Vector2.Rotate(edgeLine, MathHelper.Pi + angle / 2 + direction.ToAngle()),
            from + direction.PerpendicularClockwise() * width / 2
        ];
    }

    public static void DrawDashAttack(
        Vector2 from,
        Vector2 direction,
        float dashDistance,
        float dashWidth,
        float attackDistance,
        float attackEdgeAngle,
        float attackEdgeLength,
        float attackEdgeWidth,
        float traceDuration,
        Color traceStartColor,
        Color traceEndColor)
    {
        var bladeVertices = ArchVertices(
            from,
            direction,
            dashDistance + attackDistance,
            dashWidth,
            attackEdgeAngle,
            attackEdgeLength);

        var nextPosition = from + direction * dashDistance;
        Game.AnimationsPool.Add(new LineTrace(
            new Transform2 { Position = from },
            nextPosition,
            dashWidth,
            traceDuration,
            traceStartColor,
            traceEndColor));
       
        Game.AnimationsPool.Add(new LineTrace(
            new Transform2 { Position = bladeVertices[2] }, 
            bladeVertices[1], 
            attackEdgeWidth, 
            traceDuration, 
            traceStartColor, 
            traceEndColor, 
            1));
        Game.AnimationsPool.Add(new LineTrace(
            new Transform2 { Position = bladeVertices[2] }, 
            bladeVertices[3], 
            attackEdgeWidth, 
            traceDuration, 
            traceStartColor, 
            traceEndColor, 
            1));
    }
}