using Microsoft.Xna.Framework;
using System;
using SolsDawn.Core;

public static class SDMath
{
    /// <summary>
    /// Determines if two 2D lines intersect and calculates the intersection point.
    /// </summary>
    /// <param name="position1">The starting point of the first line.</param>
    /// <param name="direction1">The direction vector of the first line.</param>
    /// <param name="position2">The starting point of the second line.</param>
    /// <param name="direction2">The direction vector of the second line.</param>
    /// <param name="intersection">The resulting intersection point if successful; otherwise, Vector2.Zero.</param>
    /// <returns>True if the lines intersect; false if they are parallel.</returns>
    public static bool TryFindIntersection(
        Vector2 position1, 
        Vector2 direction1, 
        Vector2 position2, 
        Vector2 direction2, 
        out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        // 2D perp dot product of the direction vectors
        float denominator = direction1.X * direction2.Y - direction1.Y * direction2.X;

        // If the denominator is close to zero, the lines are parallel
        if (Math.Abs(denominator) < float.Epsilon)
        {
            return false;
        }

        // Vector from position1 to position2
        float dx = position2.X - position1.X;
        float dy = position2.Y - position1.Y;

        // Solve for the parameter t along the first line
        float t = (dx * direction2.Y - dy * direction2.X) / denominator;

        // Calculate the intersection point
        intersection = position1 + direction1 * t;
        return true;
    }


    public static Vector2 MoveTo(Vector2 start, Vector2 end, float maxShift)
    {
        var remain = end - start;
        var direction = Vector2.Zero;
        if (remain != Vector2.Zero)
        {
            direction = Vector2.Normalize(remain);
        }

        var delta = direction * maxShift;
        if (delta.LengthSquared() >= remain.LengthSquared())
        {
            return end;
        }
        return start + delta;
    }
}