using System.Collections.Generic;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace SolsDawn.Core.Logic.Gameplay;

public record Parry(Fixture Parrier, Fixture Parried, Contact Contact);
public record Attack(Fixture Hitter, Fixture Hitted, Contact Contact);

public class Intentions 
{
    public bool Resolving { get; private set; }
    private List<object> _intentionsQueue = new();
    private List<object> _lateIntentionsQueue = new();

    public void AddIntention(object intention)
    {
        if (Resolving)
        {
            _lateIntentionsQueue.Add(intention);
        }
        else
        {
            _intentionsQueue.Add(intention);
        }
    }

    public void Resolve()
    {
        Resolving = true;
        ResolveLogic();
        (_intentionsQueue, _lateIntentionsQueue) = (_lateIntentionsQueue, _intentionsQueue);
        _lateIntentionsQueue.Clear();
        Resolving = false;
    }

    private void ResolveLogic()
    {
        
    }
}