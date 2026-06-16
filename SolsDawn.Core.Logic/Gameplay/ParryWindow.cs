using MonoGame.Extended;
using MoonSharp.Interpreter;
using SolsDawn.Core.Logic.Configs;

namespace SolsDawn.Core.Logic.Gameplay;

public class ParryWindow(
    GameObject go,
    ParryType type,
    LuaRoutine routine,
    DynValue determineParried,
    LuaEvent parriedEvent)
    : Component<ParryWindow>(go)
{
    public readonly ParryType Type = type;
    public readonly LuaRoutine Routine = routine;
    public readonly LuaEvent ParriedEvent = parriedEvent;

    public bool Determine(CollisionShape2D collider) => Routine.Script.Call(determineParried, collider).Boolean;
    
    public override void Dispose()
    {
    }
}