using System;
using System.Collections.Generic;
using System.Linq;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public class EventRace
{
    public Job OnFinish(JobMethod method) => Finished.OnFire(method);
    public Job OnCancel(JobMethod method) => Finished.OnCancel(method);
    public Job OnEnd(JobMethod method) => Finished.OnEnd(method);
    public void OnFinish(Action action) => Finished.OnFire(action);
    public void OnCancel(Action action) => Finished.OnCancel(action);
    public void OnEnd(Action action) => Finished.OnEnd(action);

    public Event Finished { get; }
    public Event Winner { get; private set; }
        
    private readonly Event[] _racers;
   
    public EventRace(Job owner, IEnumerable<Event> racers)
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
        var evt = new Event(Finished.Owner);
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