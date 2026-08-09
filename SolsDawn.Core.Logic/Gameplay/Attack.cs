using System.Threading.Tasks;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

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

public delegate Task HitByPlayerReaction(HitByPlayerContext context);

public delegate bool HitByPlayerPredicate(HitByPlayerContext context);

public class PlayerAttack : Component<PlayerAttack>
{
    public Transform2 Transform => GameObject.Transform;
    
    public readonly AttackType Type;
    public readonly Routine Routine;
    public readonly HitByPlayerReaction HitExecuter;
    public readonly HitByPlayerPredicate HitDeterminer;
    public readonly ParryReaction ParryExecuter;
    private readonly Collider _collider;
    
    public PlayerAttack(
        GameObject go,
        AttackType type,
        Routine routine,
        HitByPlayerReaction hitExecuter,
        HitByPlayerPredicate hitDeterminer,
        ParryReaction parryExecuter) : base(go)
    {
        Type = type;
        Routine = routine;
        HitExecuter = hitExecuter;
        HitDeterminer = hitDeterminer;
        ParryExecuter = parryExecuter;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.OnCollision += (sender, other, contact) =>
        {
            if (other.Tag is not Collider collider)
                return false;
            if (collider.GameObject.TryGetComponent(out ParryWindow parryWindow))
            {
                var context = new ParryContext(this, parryWindow, sender, other, contact);
                IntentionsPool.AddIntention(new ParryIntention(context));
            }
            else
            {
                var context = new HitByPlayerContext(this, collider, sender, other, contact);
                IntentionsPool.AddIntention(new HitByPlayerIntention(context));
            }
            return false;
        };
        _collider.Awake = false;
    }
    
    public void Start()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Awake = true;
    }
    
    public void End() => GameObject.Dispose();
    
    public override void Dispose()
    {
    }
}

public record struct HitByEnemyContext(
    EnemyAttack Attack,
    Collider Target,
    Fixture AttackFixture,
    Fixture TargetFixture,
    Contact Contact);


public delegate Task HitByEnemyReaction(HitByEnemyContext context);

public delegate bool HitByEnemyPredicate(HitByEnemyContext context);

public class EnemyAttack : Component<EnemyAttack>
{
    public Transform2 Transform => GameObject.Transform;

    public readonly AttackType Type;
    public readonly Routine Routine;
    public readonly HitByEnemyReaction HitExecuter;
    public readonly HitByEnemyPredicate HitDeterminer;
    private readonly Collider _collider;


    public EnemyAttack(
        GameObject go,
        AttackType type,
        Routine routine,
        HitByEnemyReaction hitExecuter,
        HitByEnemyPredicate hitDeterminer) : base(go)
    {
        Type = type;
        Routine = routine;
        HitExecuter = hitExecuter;
        HitDeterminer = hitDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.OnCollision += (sender, other, contact) =>
        {
            if (other.Tag is not Collider collider)
                return false;
            var context = new HitByEnemyContext(this, collider, sender, other, contact);
            IntentionsPool.AddIntention(new HitByEnemyIntention(context));
            return false;
        };
        _collider.Awake = false;
    }

    public void Start()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Awake = true;
    }
    
    public void End() => GameObject.Dispose();

    public override void Dispose()
    {
    }
}