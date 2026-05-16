using Microsoft.Xna.Framework;

namespace SolsDawn.Core.WorldSpace;

public static class Space
{
    //Y reverse. -Y up; +Y down
    public static Point PointUp => new(0, -1);
    public static Point PointDown => new(0, 1);
    public static Point PointLeft => new(-1, 0);
    public static Point PointRight => new(1, 0);
    public static Vector2 Vector2Up => new(0, -1);
    public static Vector2 Vector2Down => new(0, 1);
    public static Vector2 Vector2Left => new(-1, 0);
    public static Vector2 Vector2Right => new(1, 0);
    
    public static Direction GetGlobalDirection(Direction direction, Direction lookDirection)
    {
        return (Direction)(((int)direction + (int)lookDirection) % 4);
    }

    public static Direction GetFacingSideDirection(Direction fromDirection, Direction objectLookDirection)
    {
        //fromDirection+2 get the global direction of object's side.
        //-objectLookDirection transform global to local (we expect local side direction) 
        return (Direction)(((int)fromDirection + 2 - (int)objectLookDirection) % 4);
    }
}