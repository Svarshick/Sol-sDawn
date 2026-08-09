using System.Threading.Tasks;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public abstract class State
{
    public virtual void Enter(State from)
    {
    }

    public virtual Task Update() => Task.CompletedTask;

    public virtual void Exit(State to)
    {
    }

    public static void Intend(GameObject source, State state) => IntentionsPool.AddIntention(new EnterStateIntention(source, state));
}