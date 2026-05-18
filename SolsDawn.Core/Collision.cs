using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;

namespace SolsDawn.Core;

public static class Collision
{
    public static class LayerName
    {
        public const string Default = CollisionWorld2D.DefaultLayerName;
        public const string Player = "player";
        public const string Enemy = "enemy";
    }

    public static readonly CollisionWorld2D World;
    public static readonly Layer DefaultLayer = new(new SpatialHash(new SizeF(64f, 64f)));
    public static readonly Layer PlayerLayer = new(new SpatialHash(new SizeF(64f, 64f)));
    public static readonly Layer EnemyLayer = new(new SpatialHash(new SizeF(64f, 64f)));

    static Collision()
    {
        World = new CollisionWorld2D(DefaultLayer);
        World.AddLayer(LayerName.Player, PlayerLayer);
        World.AddLayer(LayerName.Enemy, EnemyLayer);
    }
}

public sealed class Collider : Component<Collider>, ICollisionActor
{
    public CollisionShape2D Shape { get; set; }
    public int Id { get; }
    
    public Collider(GameObject go, int id, string layer, CollisionShape2D shape = default) : base(go)
    {
        Id = id;
        Shape = shape;
        Collision.World.Insert(this, layer);
    }

    public override void Dispose()
    {
        Collision.World.Remove(this);
    }
}

public static class CollisionExtensions
{
}