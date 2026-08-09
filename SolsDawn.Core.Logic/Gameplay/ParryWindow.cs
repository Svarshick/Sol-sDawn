using System.Threading.Tasks;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

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

public delegate Task ParryReaction(ParryContext context);

public delegate bool ParryPredicate(ParryContext context);

public class ParryWindow : Component<ParryWindow>
{
    public Transform2 Transform => GameObject.Transform;

    public readonly ParryType Type;
    public readonly Routine Routine;
    public readonly Event Parried;
    public readonly ParryReaction ParryExecuter;
    //TODO: dangerous. Expected independent from Routine and runs in Intentions stage.
    //But could capture and trigger Routine events (in theory)
    public readonly ParryPredicate ParryDeterminer;
    private readonly Collider _collider;
    
    public ParryWindow(
        GameObject go,
        ParryType type,
        Routine routine,
        ParryReaction parryExecuter,
        ParryPredicate parryDeterminer) : base(go)
    {
        Type = type;
        Routine = routine;
        Parried = new(routine);
        ParryExecuter = parryExecuter;
        ParryDeterminer = parryDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Awake = false;
    }
    
    public void Destroy()
    {
        _collider.Awake = false;
        Parried.Cancel();
        GameObject.Dispose();
    }
    
    public void Open()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Awake = true;
    }
    
    public override void Dispose()
    {
    }
}