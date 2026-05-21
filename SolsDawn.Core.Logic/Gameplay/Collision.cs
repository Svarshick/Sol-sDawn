using System.Collections.Generic;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Collision
{
    public static class LayerName
    {
        public const string Default = CollisionWorld2D.DefaultLayerName;
        public const string Player = "player";
        public const string Enemy = "enemy";
        public const string Parry = "parry";
    }

    public static readonly CollisionWorld2D World;
    public static readonly Layer DefaultLayer = new(new SpatialHash(new SizeF(64f, 64f)));
    public static readonly Layer PlayerLayer = new(new SpatialHash(new SizeF(64f, 64f)));
    public static readonly Layer EnemyLayer = new(new SpatialHash(new SizeF(64f, 64f)));
    public static readonly Layer ParryLayer = new(new SpatialHash(new SizeF(64f, 64f)));

    static Collision()
    {
        World = new CollisionWorld2D(DefaultLayer);
        World.AddLayer(LayerName.Player, PlayerLayer);
        World.AddLayer(LayerName.Enemy, EnemyLayer);
        World.AddLayer(LayerName.Parry, ParryLayer);
    }

    public static void Overlap(CollisionShape2D shape, string layerName, IList<GameObject> gameObject)
    {
        foreach (var actor in World.QueryCandidates(shape.BoundingBox, layerName))
        {
            if (shape.TryGetCollision(actor.Shape, out _)  && actor is Collider collider)
            {
                gameObject.Add(collider.GameObject);
            }
        }
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