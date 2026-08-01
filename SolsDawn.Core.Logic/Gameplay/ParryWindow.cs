using MonoGame.Extended;
using MoonSharp.Interpreter;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using SolsDawn.Core.Logic.Gameplay.Lua;

namespace SolsDawn.Core.Logic.Gameplay;

public enum ParryType
{
    Blade,
    Fire,
}

public class ParryWindow : Component<ParryWindow>
{
    //API

    public LuaEvent parried => ParriedEvent;
    public Transform2 transform => GameObject.Transform;
    public void open() => Open();
    public void destroy() => GameObject.Dispose();
    
    //INTERNAL
    
    [MoonSharpHidden] public readonly ParryType Type;
    [MoonSharpHidden] public readonly LuaRoutine Routine;
    [MoonSharpHidden] public readonly LuaEvent ParriedEvent;
    [MoonSharpHidden] private readonly DynValue _parryExecuter;
    [MoonSharpHidden] private readonly DynValue _parryDeterminer;
    [MoonSharpHidden] private readonly Collider _collider;
    
    [MoonSharpHidden]
    public ParryWindow(
        GameObject go,
        ParryType type,
        LuaRoutine routine,
        DynValue parryExecuter,
        DynValue parryDeterminer,
        Intentions intentions) : base(go)
    {
        Type = type;
        Routine = routine;
        ParriedEvent = new(routine);
        _parryExecuter = parryExecuter;
        _parryDeterminer = parryDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Body.OnCollision += (sender, other, contact) 
            => intentions.AddIntention(new Parry(sender, other, contact));
        _collider.Body.Awake = false;
    }
    
    [MoonSharpHidden]
    public bool Determine(CollisionShape2D collider) => Routine.Script.Call(_parryDeterminer, collider).Boolean;

    [MoonSharpHidden]
    public void Execute()
    {
        Routine.CreateSubroutine(_parryExecuter);
        _collider.Body.Awake = false;
        ParriedEvent.Fire();
    }

    [MoonSharpHidden]
    public void Open()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Body.Awake = true;
    }
    
    [MoonSharpHidden]
    public override void Dispose()
    {
        ParriedEvent.Cancel();
    }
}