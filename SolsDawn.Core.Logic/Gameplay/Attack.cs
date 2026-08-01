using MonoGame.Extended;
using MoonSharp.Interpreter;
using SolsDawn.Core.Logic.Gameplay.Lua;

namespace SolsDawn.Core.Logic.Gameplay;

public enum AttackType
{
    Blade,
    Fire,
}

public class PlayerAttack : Component<ParryWindow>
{
    //API

    //public MultiLuaEvent attacked
    public Transform2 transform => GameObject.Transform;
    public void start() => Start();
    public void end() => GameObject.Dispose();
    
    //INTERNAL
    
    [MoonSharpHidden] public readonly AttackType Type;
    [MoonSharpHidden] public readonly LuaRoutine Routine;
    [MoonSharpHidden] private readonly DynValue _hitExecuter;
    [MoonSharpHidden] private readonly DynValue _hitDeterminer;
    [MoonSharpHidden] private readonly DynValue _parryExecuter;
    [MoonSharpHidden] private readonly Collider _collider;
    
    [MoonSharpHidden]
    public PlayerAttack(
        GameObject go,
        AttackType type,
        LuaRoutine routine,
        DynValue hitExecuter,
        DynValue hitDeterminer,
        DynValue parryExecuter) : base(go)
    {
        Type = type;
        Routine = routine;
        _hitExecuter = hitExecuter;
        _hitDeterminer = hitDeterminer;
        _parryExecuter = parryExecuter;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Body.Awake = false;
    }
    
    [MoonSharpHidden]
    public bool DetermineHit(CollisionShape2D collider) => Routine.Script.Call(_hitDeterminer, collider).Boolean;
    
    [MoonSharpHidden]
    public void Hit() { }
    
    [MoonSharpHidden]
    public void Parry() { }

    [MoonSharpHidden]
    public void Start()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Body.Awake = true;
    }
    
    [MoonSharpHidden]
    public override void Dispose()
    {
    }
}


public class EnemyAttack : Component<ParryWindow>
{
    //API

    //public MultiLuaEvent attacked
    public Transform2 transform => GameObject.Transform;
    public void start() => Start();
    public void end() => GameObject.Dispose();
    
    //INTERNAL
    
    [MoonSharpHidden] public readonly AttackType Type;
    [MoonSharpHidden] public readonly LuaRoutine Routine;
    [MoonSharpHidden] private readonly DynValue _hitExecuter;
    [MoonSharpHidden] private readonly DynValue _hitDeterminer;
    [MoonSharpHidden] private readonly Collider _collider;
    
    [MoonSharpHidden]
    public EnemyAttack(
        GameObject go,
        AttackType type,
        LuaRoutine routine,
        DynValue hitExecuter,
        DynValue hitDeterminer) : base(go)
    {
        Type = type;
        Routine = routine;
        _hitExecuter = hitExecuter;
        _hitDeterminer = hitDeterminer;
        _collider = GameObject.GetComponent<Collider>() ?? throw new ComponentNotFoundException<Collider>();
        _collider.Body.Awake = false;
    }
    
    [MoonSharpHidden]
    public bool DetermineHit(CollisionShape2D collider) => Routine.Script.Call(_hitDeterminer, collider).Boolean;
    
    [MoonSharpHidden]
    public void Hit() { }

    [MoonSharpHidden]
    public void Start()
    {
        if (GameObject.IsDisposed)
            return;
        _collider.Body.Awake = true;
    }
    
    [MoonSharpHidden]
    public override void Dispose()
    {
    }
}