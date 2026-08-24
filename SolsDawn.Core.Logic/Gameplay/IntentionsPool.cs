using System;
using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract record Intention;

public record EnterStateIntention(GameObject Object, State State) : Intention;
public record ParryIntention(ParryContext Context) : Intention;
public record HitByPlayerIntention(HitByPlayerContext Context) : Intention;
public record HitByEnemyIntention(HitByEnemyContext Context) : Intention; 

public class IntentionsPool
{
    private List<Intention> _intentionsQueue = new();
    private List<Intention> _lateIntentionsQueue = new();

    public bool IsResolving { get; private set; }
    public Action<List<Intention>>? ResolveLogic { get; set; }
    
    public void AddIntention(Intention intention)
    {
        if (IsResolving)
        {
            _lateIntentionsQueue.Add(intention);
            Console.WriteLine($"[WARNING] Intention while resolving: {intention}");
        }
        else
        {
            _intentionsQueue.Add(intention);
        }
    }

    public void Resolve()
    {
        IsResolving = true;
        ResolveLogic?.Invoke(_intentionsQueue);
        (_intentionsQueue, _lateIntentionsQueue) = (_lateIntentionsQueue, _intentionsQueue);
        _lateIntentionsQueue.Clear();
        IsResolving = false;
    } 
}