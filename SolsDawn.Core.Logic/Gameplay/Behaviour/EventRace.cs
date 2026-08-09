using System;
using System.Collections.Generic;
using System.Linq;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class EventRace
{
    public Routine OnFinish(Callback callback) => Finished.OnFire(callback);
    public Routine OnCancel(Callback callback) => Finished.OnCancel(callback);
    public Routine OnEnd(Callback callback)    => Finished.OnEnd(callback);
    public void OnFinish(Action callback) => Finished.OnFire(callback);
    public void OnCancel(Action callback) => Finished.OnCancel(callback);
    public void OnEnd(Action callback) => Finished.OnEnd(callback);

    public Event Finished { get; }
    public Event Winner { get; private set; }
        
    private readonly Event[] _racers;
   
    public EventRace(Routine owner, IEnumerable<Event> racers)
    {
        Finished = new(owner);
        _racers = racers.ToArray();
        
        foreach (var p in _racers)
        {
            p.OnFire(() => OnParticipantFire(p));
            p.OnCancel(() => OnParticipantCancel(p));
        }
    }

    private void OnParticipantFire(Event participant)
    {
        if (Finished.IsEnded)
            return;
        Winner = participant;
        Finished.Fire();
    }

    private void OnParticipantCancel(Event participant)
    {
        if (Finished.IsEnded)
            return;
        foreach (var e in _racers)
        {
            if (e.State == EventState.Pending)
                return;
        }

        Finished.Cancel();
    }

    public Event OnWinner(Event racer)
    {
        var evt = new Event(Finished.OwnerRoutine);
        Finished.OnFire(() =>
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
        Finished.OnCancel(evt.Cancel);
        return evt;
    }
}