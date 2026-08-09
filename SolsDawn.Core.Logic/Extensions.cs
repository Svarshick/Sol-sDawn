using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

namespace SolsDawn.Core.Logic;

public static class Extensions
{
    public static void DrawPolygon(
        this SpriteBatch spriteBatch,
        IReadOnlyList<Vector2> vertices,
        Vector2 position,
        float rotation,
        Color color,
        float thickness,
        float layerDepth = 0.0f)
    {
        if (vertices.Count < 3)
            return;

        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);

        var previousX = vertices[^1].X * cos - vertices[^1].Y * sin + position.X;
        var previousY = vertices[^1].X * sin + vertices[^1].Y * cos + position.Y;
        for (int i = 0; i < vertices.Count; i++)
        {
            var currentX = vertices[i].X * cos - vertices[i].Y * sin + position.X;
            var currentY = vertices[i].X * sin + vertices[i].Y * cos + position.Y;
            spriteBatch.DrawLine(previousX, previousY, currentX, currentY, color, thickness, layerDepth);
            previousX = currentX;
            previousY = currentY;
        }
    }

    public static void DrawBody(
        this SpriteBatch spriteBatch,
        Body body,
        Color color)
    {
        spriteBatch.DrawFixtures(body.FixtureList, body.Position, body.Rotation, color);
    }
    
    public static void DrawFixtures(
        this SpriteBatch spriteBatch,
        FixtureCollection fixtureList,
        Vector2 position,
        float rotation,
        Color color)
    {
        foreach (var fixture in fixtureList)
        {
            spriteBatch.DrawShape(fixture.Shape, position, rotation, color);
        }
    }

    public static void DrawShape(
        this SpriteBatch spriteBatch,
        Shape shape,
        Vector2 position,
        float rotation,
        Color color)
    {
        switch (shape)
        {
            case PolygonShape polygonShape:
            spriteBatch.DrawPolygon(
                polygonShape.Vertices,
                position,
                rotation,
                color,
                0.05f);
            break;
            case CircleShape circleShape:
            spriteBatch.DrawCircle(
                position,
                circleShape.Radius,
                20,
                color,
                0.05f);
            break;
        }
    }

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToAngle(this Vector2 vector) => (float)Math.Atan2(vector.Y, vector.X);*/
}