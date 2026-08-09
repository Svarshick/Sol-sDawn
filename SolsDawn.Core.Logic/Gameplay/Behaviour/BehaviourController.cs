using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public static class BehaviourController
{
    public static bool IsResolving { get; private set; }

    public static Player Player { get; set; }
    public static Routine MainRoutine { get; } = new (Configs.Behaviour.Init);
    
    private static List<Routine> _deferredRoutines = new();
    private static List<Routine> _deferredRoutinesBuff = new();
    
    public static void DeferRoutine(Routine routine)
    {
        if (IsResolving)
        {
            _deferredRoutinesBuff.Add(routine);
        }
        else
        {
            _deferredRoutines.Add(routine);
        }
    }
    
    public static void Update()
    {
        IsResolving = true;
        ResolveLogic();
        (_deferredRoutines, _deferredRoutinesBuff) = (_deferredRoutinesBuff, _deferredRoutines);
        _deferredRoutinesBuff.Clear();
        IsResolving = false;
    }

    private static void ResolveLogic()
    {
        foreach (var routine in _deferredRoutines)
        {
            routine.Update();
        }

        Player.StateRoutine.Update();
        MainRoutine.Update();
    }
}