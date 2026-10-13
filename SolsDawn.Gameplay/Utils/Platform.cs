using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Gameplay.Utils;

public class Platform : Component
{
    public readonly float Width;
    public readonly Collider Collider;
    public readonly EdgeShape _edge;
    public Vector2 Point1 => Collider._body.GetWorldPoint(_edge.Vertex1);
    public Vector2 Point2 => Collider._body.GetWorldPoint(_edge.Vertex2);

    public float Radius
    {
        get => _edge.Radius;
        set => _edge.Radius = value;
    }
    
    
    public Platform(GameObject go, float width) : base(go, true)
    {
        if (width <= 0)
            Log.Logic.Fatal("Width must be greater than zero");
        Width = width;
        _edge = Shapes.EdgeShape(width);
        Collider = new (go, _edge, Layer.Wall, Layer.Player, BodyType.Static);
    }
}