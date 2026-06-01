namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public abstract class State : IUpdatable
{
    public virtual void Enter(State from)
    {
    }

    public virtual void Update()
    {
        
    }

    public virtual void LateUpdate()
    {
        
    }

    public virtual void Exit(State to)
    {
    }
    
    public static EnterStateIntention Intend(GameObject source, State state) => new (source, state);
}