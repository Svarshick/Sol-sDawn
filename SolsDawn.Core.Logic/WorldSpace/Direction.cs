using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.WorldSpace;

public enum Direction
{
    //order is IMPORTANT
    Up,
    Down,
    Left,
    Right,
    Ambiguous
}

public static class DirectionExtension
{
    public static Point ToPoint(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => Space.PointUp,
            Direction.Down => Space.PointDown,
            Direction.Left => Space.PointLeft,
            Direction.Right => Space.PointRight,
            _ => throw new NotImplementedException($"Can't convert {direction.ToString()} to Vector2Int")
        };
    }

    public static Direction ToDirection(this Point point)
    {
        if (point.Y == 0)
        {
            if (point.X < 0)
                return Direction.Left;
            if (point.X > 0)
                return Direction.Right;
        }

        if (point.X == 0)
        {
            if (point.Y < 0)
                return Direction.Up;
            if (point.Y > 0)
                return Direction.Down;
        }

        return Direction.Ambiguous;
    }

    public static char ToArrow(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => '↑',
            Direction.Down => '↓',
            Direction.Left => '←',
            Direction.Right => '→',
            _ => '?'
        };
    }

    public static Direction GetOpposite(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.Ambiguous
        };
    }
}

public static class DirectionUtils
{
    public static Direction Random()
    {
        var index = System.Random.Shared.Next(0, 3);
        return (Direction)index;
    }
}
