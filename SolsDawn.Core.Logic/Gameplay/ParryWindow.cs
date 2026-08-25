using System.Threading.Tasks;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public enum ParryType
{
    Blade,
    Fire,
}

public record struct ParryContext(
    PlayerAttack Attack,
    ParryWindow ParryWindow,
    Fixture AttackFixture,
    Fixture ParryWindowFixture,
    Contact Contact);

public delegate Job ParryReaction(ParryContext context);
public delegate bool ParryPredicate(ParryContext context);

public class ParryWindow : Component
{
    public Transform2 Transform => GameObject.Transform;

    public readonly ParryType Type;
    public readonly Job Job;
    public readonly Event Parried;
    private readonly ParryReaction? _parryExecuter;
    private readonly ParryPredicate? _parryDeterminer;
    private readonly Collider _collider;

    public ParryWindow(
        GameObject go,
        ParryType type,
        Job job,
        ParryReaction? parryExecuter,
        ParryPredicate? parryDeterminer) : base(go, true)
    {
        Type = type;
        Job = job;
        Parried = new(job);
        _parryExecuter = parryExecuter;
        _parryDeterminer = parryDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Enabled = false;
    }

    public Job Execute(ParryContext context)
    {
        if (_parryExecuter is null)
            return Job.CompletedJob;
        using (JobContext.Use(Job))
        {
            return _parryExecuter(context);
        }
    }
        
    public bool Determine(ParryContext context)
    {
        if (_parryDeterminer is null)
            return true;
        using (JobContext.Use(Job))
        {
            return _parryDeterminer(context);
        }
    }

    public override void OnDestroyImmediate()
    {
        _collider.Enabled = false;
        Parried.Cancel();
    }

    public void Open()
    {
        if (GameObject.IsDestroyed)
            return;
        _collider.Enabled = true;
    }
}