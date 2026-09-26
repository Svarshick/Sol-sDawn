using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Gameplay.Utils;

public class Platform : Component
{
    public readonly float Width;
    public readonly Collider Collider;
    private readonly EdgeShape _edge;
    public Vector2 Point1 => _edge.Vertex1.Rotated(Transform.Rotation) + Transform.Position;
    public Vector2 Point2 => _edge.Vertex2.Rotated(Transform.Rotation) + Transform.Position;
    
    
    public Platform(GameObject go, float width) : base(go)
    {
        Width = width;
        _edge = Shapes.EdgeShape(width);
        Collider = new (go, _edge, Layer.Wall, Layer.Player, BodyType.Kinematic);
    }
}