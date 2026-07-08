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
    public float Radius;
    public float Velocity;

    public float ExplosionRadius;
    public Color ExplosionColor;
    public float ExplosionTraceDuration;
}

/*public class Orb : Component<Orb>, IUpdatable, IDrawable
{
    public readonly OrbStats Stats;
    
    private readonly Collider _collider;
    private readonly HP _hp;
    
    public State State { get; private set; }

    public Orb(GameObject go, OrbStats stats) : base(go)
    {
        Stats = stats;
        
        _collider = go.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _hp = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();

        State = new FollowState(this);
    }

    public override void Dispose()
    {
    }

    public void Update()
    {
        var bounds = new BoundingCircle2D(GameObject.Transform.Position, Stats.Radius);
        _collider.Shape = new CollisionShape2D(bounds);
        State.Update();
    }

    public void LateUpdate()
    {
    }
    
    public void Enter(State state)
    {
        State.Exit(state);
        state.Enter(State);
        State = state;
    }

    public void Draw()
    {
        Game.SpriteBatch.DrawCircle(GameObject.Transform.Position, Stats.Radius, 20, Stats.Color, Stats.Radius);
    }
    
    public void BeDamaged(int value)
    {
        Console.WriteLine($"[Player] Damaged : {value}");
        _hp.Current = 0;
    }

    public class FollowState(
        Orb orb) 
        : State
    {
        public Vector2 Target;

        public override void Update()
        {
            var shift = (float)(Time.ElapsedGameTime.TotalSeconds * orb.Stats.Velocity);
            orb.GameObject.Transform.Position = SDMath.MoveTo(orb.GameObject.Transform.Position, Target, shift);

            if (orb.GameObject.Transform.Position == Target)
            {
                orb.GameObject.Dispose();
                return;
            }

            var circle = new BoundingCircle2D(orb.GameObject.Transform.Position, orb.Stats.Radius);
            var shape = new CollisionShape2D(circle);
            var targets = new List<GameObject>();
            Collision.Overlap(shape, Collision.LayerName.Player, targets);
            if (targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(orb.GameObject, targets, 1));
                orb.Dispose();
            }
        }
    }

    public class ExplodeState(
        Orb orb) 
        : State
    {
        public override void Enter(State from)
        {
            var stats = orb.Stats;
            var go = orb.GameObject;
            var circle = new BoundingCircle2D(go.Transform.Position, stats.ExplosionRadius);
            var shape = new CollisionShape2D(circle);
            var targets = new List<GameObject>();
            Collision.Overlap(shape, Collision.LayerName.Player, targets);
            if (targets.Count > 0)
            {
                AffectsPool.Add(new DamageAffect(go, targets, 1));
            }

            Game.AnimationsPool.Add(new CircleTrace(
                go.Transform, stats.ExplosionRadius,
                20,
                stats.ExplosionRadius,
                stats.ExplosionTraceDuration,
                stats.ExplosionColor,
                Color.Transparent));

            go.Dispose();
        }
    }
}*/