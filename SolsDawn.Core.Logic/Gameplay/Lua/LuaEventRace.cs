using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

[MoonSharpUserData]
public class LuaEventRace
{
    //API
    public LuaRoutine onFinish(DynValue callback) => FinishEvent.OnFire(callback);
    public LuaRoutine onCancel(DynValue callback) => FinishEvent.OnCancel(callback);
    public LuaRoutine onEnd(DynValue callback)    => FinishEvent.OnEnd(callback);

    public LuaEvent onWinner(LuaEvent racer) => OnWinner(racer);

    public LuaEvent finished => FinishEvent; 
    
    //INTERNAL
    
    [MoonSharpHidden] public LuaEvent FinishEvent { get; }
    [MoonSharpHidden] public LuaEvent Winner { get; private set; }
        
    [MoonSharpHidden] private readonly LuaEvent[] _racers;
    
    [MoonSharpHidden]
    public LuaEventRace(LuaRoutine owner, IEnumerable<LuaEvent> racers)
    {
        FinishEvent = new(owner);
        _racers = racers.ToArray();
        
        foreach (var p in _racers)
        {
            p.OnFire(() => OnParticipantFire(p));
            p.OnCancel(() => OnParticipantCancel(p));
        }
    }

    [MoonSharpHidden]
    private void OnParticipantFire(LuaEvent participant)
    {
        if (FinishEvent.IsEnded)
            return;
        Winner = participant;
        FinishEvent.Fire();
    }

    [MoonSharpHidden]
    private void OnParticipantCancel(LuaEvent participant)
    {
        if (FinishEvent.IsEnded)
            return;
        foreach (var e in _racers)
        {
            if (e.State == LuaEventState.Pending)
                return;
        }

        FinishEvent.Cancel();
    }

    [MoonSharpHidden]
    public LuaEvent OnWinner(LuaEvent racer)
    {
        var evt = new LuaEvent(FinishEvent.OwnerRoutine);
        FinishEvent.OnFire(() =>
        {
            if (Winner == racer)
            {
                evt.Fire();
            }
            else
            {
                evt.Cancel();
            }
        });
        FinishEvent.OnCancel(evt.Cancel);
        return evt;
    }
}