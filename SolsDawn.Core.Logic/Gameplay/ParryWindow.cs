using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public record struct BladeParryContext(
    PlayerBladeParryingAttack Attack,
    BladeParryWindow ParryWindow,
    Fixture AttackFixture,
    Fixture ParryWindowFixture,
    Contact Contact);

public delegate Job BladeParryReaction(BladeParryContext context);
public delegate bool BladeParryPredicate(BladeParryContext context);

public class BladeParryWindow : Component
{
    public readonly Entity? Owner;
    public readonly Job Job;
    public readonly Event Parried;

    private readonly BladeParryReaction _parriedReaction;
    private readonly BladeParryPredicate? _parryDeterminer;
    private readonly Collider _collider;

    public BladeParryWindow(
        GameObject go,
        Entity? owner,
        Job job, 
        Shape shape,
        BladeParryReaction parriedReaction,
        BladeParryPredicate? parryDeterminer = null) : base(go)
    {
        Owner = owner;
        Job = job;
        Parried = new(job);
        _parriedReaction = parriedReaction;
        _parryDeterminer = parryDeterminer;

        _collider = new Collider(go, shape, Collision.BladeParry, Collision.BladeAttack, BodyType.Dynamic, true);
        _collider.Enabled = false;
    }
    
    public void Open()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }
    
    public bool Determine(BladeParryContext context)
    {
        if (_parryDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _parryDeterminer(context);
        }
    }

    public Job Execute(BladeParryContext context)
    {
        using (JobContext.Use(Job))
        {
            return _parriedReaction(context);
        }
    }
    
    public override void OnDestroyImmediate()
    {
        _collider.Enabled = false;
        Parried.Cancel();
    }
}

public record struct FireParryContext(Vector2 BumpPoint);

public delegate bool FireParryPredicate(FireParryContext context);
public delegate Job FireParryReaction(FireParryContext context);

public class FireParryWindow : Component
{
    public readonly Entity? Owner;
    public readonly Job Job;
    public readonly Event Parried;
    
    private readonly FireParryReaction _parryBumpReaction;
    private readonly FireParryReaction _parriedReaction;
    private readonly FireParryPredicate? _parryDeterminer;
    private readonly Collider _collider;

    public FireParryWindow(
        GameObject go,
        Entity? owner,
        Job job, 
        Shape shape,
        FireParryReaction parryBumpReaction,
        FireParryReaction parriedReaction,
        FireParryPredicate? parryDeterminer = null) : base(go)
    {
        Owner = owner;
        Job = job;
        Parried = new(job);
        _parryBumpReaction = parryBumpReaction;
        _parriedReaction = parriedReaction;
        _parryDeterminer = parryDeterminer;
        
        _collider = new Collider(go, shape, Collision.FireParry, Collision.FireAttack, BodyType.Dynamic, true);
        _collider.Enabled = false;
    }

    public void Open()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }
    
    public bool Determine(FireParryContext context)
    {
        if (_parryDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _parryDeterminer(context);
        }
    }
    
    public (Job Parrier, Job Parried, Job ParryBump) Execute(FireParryContext context, FireParryReaction parrierReaction)
    {
        //uses Job context even for parrier (that should use parrier Entity.Job)
        //temp dirt solution
        using (JobContext.Use(Job)) 
        {
            var parrier = parrierReaction(context);
            var parried = _parriedReaction(context);
            var parryBump = _parryBumpReaction(context);
            return (parrier, parried, parryBump);
        }
    }
    
    public override void OnDestroyImmediate()
    {
        _collider.Enabled = false;
        Parried.Cancel();
    }
}