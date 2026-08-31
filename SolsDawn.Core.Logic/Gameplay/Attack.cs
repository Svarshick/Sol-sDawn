using System;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public record struct HitContext(
    Attack Attack,
    Entity Target,
    Fixture AttackFixture,
    Fixture TargetFixture,
    Contact Contact);

public delegate Job HitReaction(HitContext context);
public delegate bool HitPredicate(HitContext context);

public class Attack : Component
{
    public readonly Job Job;
    protected readonly Collider _collider;
    private readonly HitPredicate? _hitDeterminer;
    private readonly HitReaction? _hitExecuter;

    public Attack(
        GameObject go, 
        Job job,
        Shape shape,
        Category selfLayer,
        Category collidesLayer,
        HitPredicate? hitDeterminer,
        HitReaction? hitExecuter) : base(go, true)
    {
        Job = job;
        _collider = new Collider(go, shape, selfLayer, collidesLayer, BodyType.Dynamic, true);
        _collider.OnCollision += OnCollision;
        _collider.Enabled = false;
        _hitDeterminer = hitDeterminer;
        _hitExecuter = hitExecuter;
    }

    public virtual bool OnCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is not Collider collider)
            return true;
        
        if (collider.GameObject.TryGetComponent(out Entity entity))
        {
            var context = new HitContext(this, entity, sender, other, contact);
            Game.CollisionsPool.Add(new HitCollision(context));
        }
        else
        {
            Console.WriteLine("[Warning] attack collides collider without entity");
        }
        
        return true;
    }

    public void Open()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }

    public bool DetermineHit(HitContext context)
    {
        if (_hitDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _hitDeterminer(context);
        }
    }

    public Job ExecuteHit(HitContext context)
    {
        if (_hitExecuter is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _hitExecuter(context);
        }
    }
}

public class PlayerBladeParryingAttack : Attack
{
    private readonly BladeParryReaction? _parryReaction;
    
    public PlayerBladeParryingAttack(
        GameObject go, 
        Job job,
        Shape shape,
        HitPredicate? hitDeterminer,
        HitReaction? hitExecuter,
        BladeParryReaction? parryReaction)
        : base(go, job, shape, Collision.BladeAttack, Collision.Enemy | Collision.BladeParry, hitDeterminer, hitExecuter)
    {
        _parryReaction = parryReaction;
    }

    public override bool OnCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is not Collider collider)
            return true;
        
        var resolved = false;

        if (collider.GameObject.TryGetComponent(out BladeParryWindow parryWindow))
        {
            var context = new BladeParryContext(this, parryWindow, sender, other, contact);
            Game.CollisionsPool.Add(new BladeParryCollision(context));
            resolved = true;
        }
        
        if (collider.GameObject.TryGetComponent(out Entity entity))
        {
            var context = new HitContext(this, entity, sender, other, contact);
            Game.CollisionsPool.Add(new HitCollision(context));
            resolved = true;
        }
        
        if (!resolved)
        {
            Console.WriteLine("[Warning] attack collides collider without entity");
        }

        return true;
    }

    public Job ExecuteParry(BladeParryContext context)
    {
        if (_parryReaction is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _parryReaction(context);
        }
    }
}