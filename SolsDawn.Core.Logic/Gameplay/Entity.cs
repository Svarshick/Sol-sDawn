using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract class State
{
    public virtual Job Job(Entity entity) => Pipeline.Job.CompletedJob;
}

public abstract class State<TEntity> : State
    where TEntity : Entity
{
    public sealed override Job Job(Entity entity) => Job((TEntity)entity);

    public abstract Job Job(TEntity entity);
}

public abstract class Entity : Component
{
    public readonly Collider Collider;
    
    public readonly Job? RootJob;
    public State? State { get; private set; }
    public Job? StateJob { get; private set; }

    public Entity(
        GameObject go, 
        Job? rootJob, 
        Shape shape,
        Category layer)
        : base(go, true)
    {
        RootJob = rootJob;
        Collider = new(go, shape, layer);
    }
    
    public void Enter(State state)
    {
        Log.Logic.Info($"Change state: {State} -> {state}");
        if (State is not null)
        {
            if (StateJob is null)
            {
                Log.Logic.Warning($"Previous state {State} job is null");
            }
            else if (StateJob.IsEnded)
            {
                Log.Logic.Warning($"Previous state {State} job is ended");
            }
            else
            {
                StateJob.Kill();
            }
        }

        using (JobContext.Use(RootJob))
        {
            State = state;
            JobAsyncMethodBuilder.DeferStart = true;
            try
            {
                StateJob = state.Job(this);
            }
            finally
            {
                JobAsyncMethodBuilder.DeferStart = false;
            }
        }
    }

    public void UpdateState() => StateJob?.Update();
}

public abstract class Entity<TBoard, TAnimation> : Entity 
    where TBoard : class
    where TAnimation : AnimationSet
{ 
    public readonly Animator<TAnimation> Animator;
    public readonly TBoard Board;

    public Entity(
        GameObject go, 
        Job? job,
        TBoard board, 
        TAnimation animationPlayer, 
        Shape shape,
        Category layer) 
        : base(go, job, shape, layer)
    {
        Board = board;
        Animator = new Animator<TAnimation>(go, animationPlayer);
    }
}