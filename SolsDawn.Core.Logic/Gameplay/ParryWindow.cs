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

public class ParryWindow : Component
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
        ParryPredicate parryDeterminer) : base(go, true)
    {
        Type = type;
        Routine = routine;
        Parried = new(routine);
        ParryExecuter = parryExecuter;
        ParryDeterminer = parryDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Enabled = false;
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