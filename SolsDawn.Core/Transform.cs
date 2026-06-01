using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public class Transform
{
    public Transform()
    {
        
    }
    
    public Transform(Vector2 position)
    {
        Position = position;
    }
        
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}