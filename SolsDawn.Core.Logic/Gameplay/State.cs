using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract class State
{
    public virtual void Enter(State from)
    {
    }

    public virtual Job Update() => Job.CompletedJob;

    public virtual void Exit(State to)
    {
    }
}