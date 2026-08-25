using Apos.Shapes;
using nkast.Aether.Physics2D.Collision.Shapes;

namespace SolsDawn.Core.Logic;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Painter
{
    private readonly ShapeBatch _shapeBatch;
    private readonly List<(Action DrawCommand, float Layer)> _drawCommands;

    public Painter(GraphicsDevice graphicsDevice)
    {
        _shapeBatch = new ShapeBatch(graphicsDevice);
        _drawCommands = new();
    }

    public void DoDraws()
    {
        _drawCommands.Sort((x, y) => x.Layer.CompareTo(y.Layer));

        foreach (var (drawCommand, _) in _drawCommands)
            drawCommand();

        _drawCommands.Clear();
    }

    public void Begin(
        Matrix? view = null,
        Matrix? projection = null,
        BlendState? blendState = null,
        SamplerState? samplerState = null,
        DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null)
        => _shapeBatch.Begin(view, projection, blendState, samplerState, depthStencilState, rasterizerState);

    public void End() => _shapeBatch.End();

    #region Circle

    public void DrawCircle(
        float layer,
        Vector2 center,
        float radius,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        float rotation = 0.0f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawCircle(center, radius, fill, border, thickness, rotation, aaSize, dash),
            layer));

    public void FillCircle(
        float layer,
        Vector2 center,
        float radius,
        Gradient g,
        float rotation = 0.0f,
        float aaSize = 1.5f)
        => _drawCommands.Add((
            () => _shapeBatch.FillCircle(center, radius, g, rotation, aaSize),
            layer));

    public void BorderCircle(
        float layer,
        Vector2 center,
        float radius,
        Gradient g,
        float thickness = 0.1f,
        float rotation = 0.0f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.BorderCircle(center, radius, g, thickness, rotation, aaSize, dash),
            layer));

    #endregion

    #region Rectangle

    public void DrawRectangle(
        float layer,
        Vector2 xy,
        Vector2 size,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        CornerRadii cornerRadii = default(CornerRadii),
        float rotation = 0.0f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawRectangle(xy, size, fill, border, thickness, cornerRadii, rotation, aaSize, dash),
            layer));

    public void FillRectangle(
        float layer,
        Vector2 center,
        Vector2 size,
        Gradient g,
        CornerRadii cornerRadii = default(CornerRadii),
        float rotation = 0.0f,
        float aaSize = 1.5f)
        => _drawCommands.Add((
            () => _shapeBatch.FillRectangle(new Vector2(center.X - size.X/2, center.Y - size.Y/2), size, g, cornerRadii, rotation, aaSize),
            layer));
    
    #endregion

    #region Triangle

    public void FillTriangle(
        float layer,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Gradient g,
        float rounded = 0.0f,
        float aaSize = 1.5f)
        => _drawCommands.Add((
            () => _shapeBatch.FillTriangle(a, b, c, g, rounded, aaSize),
            layer));

    public void DrawTriangle(
        float layer,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        float rounded = 0.0f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawTriangle(a, b, c, fill, border, thickness, rounded, aaSize, dash),
            layer));

    #endregion

    #region Path

    public void FillPath(
        float layer,
        Vector2[] points,
        float radius,
        Gradient g,
        PathJoin join = PathJoin.Round,
        PathCap cap = PathCap.Round,
        PathCap? capEnd = null,
        float miterLimit = 4f,
        float aaSize = 1.5f,
        bool closed = false,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.FillPath(points, radius, g, join, cap, capEnd, miterLimit, aaSize, closed, dash),
            layer));

    public void DrawPath(
        float layer,
        Vector2[] points,
        float radius,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        PathJoin join = PathJoin.Round,
        PathCap cap = PathCap.Round,
        PathCap? capEnd = null,
        float miterLimit = 4f,
        float aaSize = 1.5f,
        bool closed = false,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawPath(points, radius, fill, border, thickness, join, cap, capEnd, miterLimit, aaSize,
                closed, dash),
            layer));

    #endregion

    #region Line

    public void FillLine(
        float layer,
        Vector2 a,
        Vector2 b,
        float radius,
        Gradient g,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.FillLine(a, b, radius, g, aaSize, dash),
            layer));

    public void DrawLine(
        float layer,
        Vector2 a,
        Vector2 b,
        float radius,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawLine(a, b, radius, fill, border, thickness, aaSize, dash),
            layer));

    #endregion

    #region Ring

    public void FillRing(
        float layer,
        Vector2 center,
        float angle1,
        float angle2,
        float radius1,
        float radius2,
        Gradient g,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.FillRing(center, angle1, angle2, radius1, radius2, g, aaSize, dash),
            layer));

    public void DrawRing(
        float layer,
        Vector2 center,
        float angle1,
        float angle2,
        float radius1,
        float radius2,
        Gradient fill,
        Gradient border,
        float thickness = 0.1f,
        float aaSize = 1.5f,
        DashStyle dash = default)
        => _drawCommands.Add((
            () => _shapeBatch.DrawRing(center, angle1, angle2, radius1, radius2, fill, border, thickness, aaSize, dash),
            layer));

    #endregion

    public void BorderShape(
        float layer,
        Shape shape,
        Vector2 position,
        float rotation,
        Color color,
        float thickness = 0.1f)
    {
        switch (shape)
        {
            case PolygonShape polygonShape:
                BorderPolygon(layer, polygonShape, position, rotation, color, thickness);
                break;
            case CircleShape circleShape:
                BorderCircle(layer, position, circleShape.Radius, color, thickness);
                break;
        }
    }

    public void BorderPolygon(
        float layer,
        PolygonShape polygon,
        Vector2 position,
        float rotation,
        Color color,
        float thickness = 0.1f)
        => _drawCommands.Add((() =>
            {
                var vertices = polygon.Vertices;
                if (vertices.Count < 2)
                    return;

                var cos = MathF.Cos(rotation);
                var sin = MathF.Sin(rotation);

                var previous = new Vector2(
                    vertices[^1].X * cos - vertices[^1].Y * sin + position.X,
                    vertices[^1].X * sin + vertices[^1].Y * cos + position.Y);

                for (int i = 0; i < vertices.Count; i++)
                {
                    var current = new Vector2(
                        vertices[i].X * cos - vertices[i].Y * sin + position.X,
                        vertices[i].X * sin + vertices[i].Y * cos + position.Y
                    );
                    _shapeBatch.FillLine(previous, current, thickness/2, color);
                    previous = current;
                }
            },
            layer));

    public void FillArrow(
        float layer,
        Vector2 from,
        Vector2 direction,
        float tailLength,
        float tailWidth,
        float headLength,
        float headWidth,
        Color color)
    {
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        var perp = new Vector2(-direction.Y, direction.X);

        var tailEnd = from + direction * tailLength;
        var tip = tailEnd + direction * headLength;

        var headLeft = tailEnd + perp * (headWidth * 0.5f);
        var headRight = tailEnd - perp * (headWidth * 0.5f);

        FillLine(layer, from, tailEnd, tailWidth * 0.5f, color);
        FillTriangle(layer, tip, headLeft, headRight, color);
    }

    public void FillArrowPentagon(
        float layer,
        Vector2 from,
        Vector2 direction,
        float tailLength,
        float tailWidth,
        float headLength,
        float headWidth,
        Color color)
    {
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        var perp = new Vector2(-direction.Y, direction.X);

        var tailEnd = from + direction * tailLength;

        var v0Tip = tailEnd + direction * headLength;
        var v1HeadRight = tailEnd - perp * (headWidth * 0.5f);
        var v2TailRight = from - perp * (tailWidth * 0.5f);
        var v3TailLeft = from + perp * (tailWidth * 0.5f);
        var v4HeadLeft = tailEnd + perp * (headWidth * 0.5f);


        FillTriangle(layer, v0Tip, v4HeadLeft, v1HeadRight, color);
        
        FillTriangle(layer, v4HeadLeft, v3TailLeft, v2TailRight, color);
        FillTriangle(layer, v4HeadLeft, v2TailRight, v1HeadRight, color);
    }
}