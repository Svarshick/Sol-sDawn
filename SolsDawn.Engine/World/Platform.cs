using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using Collider = SolsDawn.Engine.Collider;
using static SolsDawn.Engine.GameplayAPI;

namespace SolsDawn.Engine;

public class Platform : Component
{
    internal readonly Collider Collider;
    public readonly EdgeShape Edge;
    public Vector2 Point1 => Transform.WorldPosition + Edge.Vertex1.Rotated(Transform.WorldRotation);
    public Vector2 Point2 => Transform.WorldPosition + Edge.Vertex2.Rotated(Transform.WorldRotation);

    internal Platform(GameObject go, float width) : base(go, true)
    {
        if (width <= 0)
            Log.Logic.Fatal("Width must be greater than zero");
        Edge = Shapes.EdgeShape(width);
        Collider = new(go, Edge, Layer.Wall);
    }
}
