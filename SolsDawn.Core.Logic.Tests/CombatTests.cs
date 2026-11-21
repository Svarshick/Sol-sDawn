using SolsDawn.Core.Logic.Combat;
using Xunit;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SolsDawn.Core.Logic.Tests;

public sealed class CombatTests
{
    [Theory]
    [InlineData(1f, 1f, 0f, 1f, 0f)]
    [InlineData(-1f, 1f, 0f, -1f, 0f)]
    [InlineData(0f, -1f, 0f, 1f, 0f)]
    [InlineData(1f, 0.5f, MathF.PI / 2, 0f, 1f)]
    [InlineData(1f, -0.5f, MathF.PI / 2, 0f, -1f)]
    public void Terminology_TangentProjectionKeepsTheSideAndHasUnitLength(float dx, float dy, float rotation, float ex, float ey)
    {
        var result = CombatMath.TangentProjection(new Vector2(dx, dy), new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)));

        Assert.Equal(ex, result.X, 4);
        Assert.Equal(ey, result.Y, 4);
    }

    [Fact]
    public void Terminology_PentagonPointsAreConvexAndCounterClockwise()
    {
        var points = CombatMath.PentagonPoints(1f, 1f, 2f);

        Assert.Equal([new Vector2(0, -1), new Vector2(1, -1), new Vector2(3, 0), new Vector2(1, 1), new Vector2(0, 1)], points);
        for (var i = 0; i < points.Length; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Length];
            var c = points[(i + 2) % points.Length];
            var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            Assert.True(cross > 0);
        }
    }

    [Theory]
    [InlineData(10f, 4f, 20f, 5f)]
    [InlineData(-4f, 0f, 20f, 0f)]
    [InlineData(100f, 0f, 20f, 20f)]
    public void CMB43_ParryPointIsHalfTheProjectionOnTheAxis(float boxX, float boxY, float distance, float expected)
    {
        var t = CombatMath.FireParryDistance(Vector2.Zero, Vector2.UnitX, [new Vector2(boxX, boxY)], distance);

        Assert.Equal(expected, t, 4);
    }

    [Fact]
    public void CMB43_NearestParryboxGivesThePoint()
    {
        var t = CombatMath.FireParryDistance(Vector2.Zero, Vector2.UnitX, [new Vector2(10, 0), new Vector2(6, 1)], 20f);

        Assert.Equal(3f, t, 4);
    }
}
