using MonoGame.Extended;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public enum AttackType
{
    Blade,
    Fire,
}

public record struct HitByPlayerContext(
    PlayerAttack Attack,
    Collider Target,
    Fixture AttackFixture,
    Fixture TargetFixture,
    Contact Contact);

public delegate Job HitByPlayerReaction(HitByPlayerContext context);
public delegate bool HitByPlayerPredicate(HitByPlayerContext context);

public class PlayerAttack : Component
{
    public Transform2 Transform => GameObject.Transform;
    
    public readonly AttackType Type;
    public readonly Job Job;
    private readonly HitByPlayerReaction? _hitExecuter;
    private readonly HitByPlayerPredicate? _hitDeterminer;
    private readonly ParryReaction? _parryExecuter;
    private readonly Collider _collider;
    
    public PlayerAttack(
        GameObject go,
        AttackType type,
        Job job,
        HitByPlayerReaction? hitExecuter,
        HitByPlayerPredicate? hitDeterminer,
        ParryReaction? parryExecuter) 
        : base(go, true)
    {
        Type = type;
        Job = job;
        _hitExecuter = hitExecuter;
        _hitDeterminer = hitDeterminer;
        _parryExecuter = parryExecuter;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.OnCollision += (sender, other, contact) =>
        {
            if (other.Tag is not Collider collider)
                return false;
            if (collider.GameObject.TryGetComponent(out ParryWindow parryWindow))
            {
                var context = new ParryContext(this, parryWindow, sender, other, contact);
                Game.IntentionsPool.AddIntention(new ParryIntention(context));
            }
            else
            {
                var context = new HitByPlayerContext(this, collider, sender, other, contact);
                Game.IntentionsPool.AddIntention(new HitByPlayerIntention(context));
            }
            return false;
        };
        _collider.Enabled = false;
    }
    
    public void Start()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }

    public bool DetermineHit(HitByPlayerContext context)
    {
        if (_hitDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _hitDeterminer(context);
        }
    }

    public Job ExecuteHit(HitByPlayerContext context)
    {
        if (_hitExecuter is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _hitExecuter(context);
        }
    }

    public Job ExecuteParry(ParryContext context)
    {
        if (_parryExecuter is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _parryExecuter(context);
        }
    }

    public void End() => Destroy();
}

public record struct HitByEnemyContext(
    EnemyAttack Attack,
    Collider Target,
    Fixture AttackFixture,
    Fixture TargetFixture,
    Contact Contact);

public delegate Job HitByEnemyReaction(HitByEnemyContext context);

public delegate bool HitByEnemyPredicate(HitByEnemyContext context);

public class EnemyAttack : Component
{
    public Transform2 Transform => GameObject.Transform;

    public readonly AttackType Type;
    public readonly Job Job;
    private readonly HitByEnemyReaction? _hitExecuter;
    private readonly HitByEnemyPredicate? _hitDeterminer;
    private readonly Collider _collider;


    public EnemyAttack(
        GameObject go,
        AttackType type,
        Job job,
        HitByEnemyReaction? hitExecuter,
        HitByEnemyPredicate? hitDeterminer) 
        : base(go, true)
    {
        Type = type;
        Job = job;
        _hitExecuter = hitExecuter;
        _hitDeterminer = hitDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.OnCollision += (sender, other, contact) =>
        {
            if (other.Tag is not Collider collider)
                return false;
            var context = new HitByEnemyContext(this, collider, sender, other, contact);
            Game.IntentionsPool.AddIntention(new HitByEnemyIntention(context));
            return false;
        };
        _collider.Enabled = false;
    }
    
    public bool DetermineHit(HitByEnemyContext context)
    {
        if (_hitDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _hitDeterminer(context);
        }
    }

    public Job ExecuteHit(HitByEnemyContext context)
    {
        if (_hitExecuter is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _hitExecuter(context);
        }
    }
    
    public void Start()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }
    
    public void End() => Destroy();
}