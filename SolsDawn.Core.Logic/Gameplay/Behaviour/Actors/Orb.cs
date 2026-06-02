using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class OrbStats
{
    public Color Color;
    [Units] public float Radius;
    [Units] public float Velocity;

    [Units] public float ExplosionRadius;
    public Color ExplosionColor;
    public float ExplosionTraceDuration;
}

public class Orb : Component<Orb>, IUpdatable, IDrawable
{
    public readonly OrbStats Stats;
    
    private Collider _collider;
    private Hp _hp;

    public Vector2 Target;

    public Orb(GameObject go, OrbStats stats) : base(go)
    {
        _collider = go.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _hp = go.GetComponent<Hp>() ?? throw new ComponentNotFoundException<Hp>();
        
        Stats = stats;
    }

    public override void Dispose()
    {
    }

    public void Update()
    {
        var shift = (float)(Time.ElapsedGameTime.TotalSeconds * Stats.Velocity);
        GameObject.Transform.Position = SDMath.MoveTo(GameObject.Transform.Position, Target, shift);

        var circle = new BoundingCircle2D(GameObject.Transform.Position, Stats.Radius);
        var shape = new CollisionShape2D(circle);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Player, targets);
        if (targets.Count > 0)
        {
            Explode();
            GameObject.Dispose();
            return;
        }
        
        var bounds = new BoundingCircle2D(GameObject.Transform.Position, Stats.Radius);
        _collider.Shape = new CollisionShape2D(bounds);
    }

    public void LateUpdate()
    {
    }

    public void Draw()
    {
        Game.SpriteBatch.DrawCircle(GameObject.Transform.Position, Stats.Radius, 20, Stats.Color, Stats.Radius);
    }
    
    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Player] Damaged : {value}");
        _hp.ChangeCurrent(0);
    }

    private void Explode()
    {
        var circle = new BoundingCircle2D(GameObject.Transform.Position, Stats.ExplosionRadius);
        var shape = new CollisionShape2D(circle);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Player, targets);
        if (targets.Count > 0)
        {
            AffectsPool.Add(new DamageAffect(GameObject, targets, 1));
        }

        Game.AnimationsPool.Add(new CircleTrace(
            GameObject.Transform, Stats.ExplosionRadius, 
            20, 
            Stats.ExplosionRadius, 
            Stats.ExplosionTraceDuration,
            Stats.ExplosionColor,
            Color.Transparent));
    }
}