using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.WorldSpace;

public class GridLayoutService
{
    public readonly Vector2 CellSize;

    public GridLayoutService(Vector2 cellSize)
    {
        CellSize = cellSize;
    }

    public Point WorldToGrid(Vector2 worldPosition)
    {
        var x = (int)Math.Floor(worldPosition.X / CellSize.X);
        var y = (int)Math.Floor(worldPosition.Y / CellSize.Y);
        return new Point(x, y);
    }

    public Vector2 GridToWorld(Point gridPosition)
    {
        var x = gridPosition.X * CellSize.X + CellSize.X / 2;
        var y = gridPosition.Y * CellSize.Y + CellSize.Y / 2;
        return new Vector2(x, y);
    }
}