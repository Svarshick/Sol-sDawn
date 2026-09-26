using System;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract class State
{
    public virtual void Enter(State? from)
    {
    }

    public virtual Job Job() => Pipeline.Job.CompletedJob;

    public virtual void Exit(State? to)
    {
    }
}

public abstract class Entity : Component
{
    public readonly Collider Collider;
    public readonly Category SelfLayer;
    public readonly Category CollidesLayer;
    
    public event Action<int, Job>? Damaged;

    public readonly Job RootJob;
    public State? State { get; private set; }
    public Job? StateJob { get; private set; }

    public Entity(
        GameObject go, 
        Job rootJob, 
        Shape shape,
        Category selfLayer,
        Category collidesLayer)
        : base(go, true)
    {
        RootJob = rootJob;
        SelfLayer = selfLayer;
        CollidesLayer = collidesLayer;
        Collider = new(go, shape, SelfLayer, CollidesLayer);
    }

    public void Damage(int damage)
    {
        if (damage < 1)
            throw new LogicException($"Damage can't be negative: {damage}");
        Damaged?.Invoke(damage, RootJob);
    }
    
    public void Enter(State state)
    {
        if (State is not null)
        {
            State.Exit(state);
            if (StateJob is null)
            {
                Console.WriteLine($"[Warning] previous state {State} job is null");
            }
            else if (StateJob.IsEnded)
            {
                Console.WriteLine($"[Warning] previous state {State} job is ended");
            }
            else
            {
                StateJob.Kill();
            }
        }

        using (JobContext.Use(RootJob))
        {
            state.Enter(State);
            State = state;
            StateJob = state.Job();
        }
    }
}

public abstract class Entity<TBoard, TAnimation> : Entity 
    where TBoard : class
    where TAnimation : AnimationSet
{ 
    public readonly Animator<TAnimation> Animator;
    public readonly TBoard Board;

    public Entity(
        GameObject go, 
        TBoard board, 
        TAnimation animationPlayer, 
        Shape shape,
        Category selfLayer,
        Category collidesLayer) 
        : base(go, GameplayAPI.CurrentJob, shape, selfLayer, collidesLayer)
    {
        Board = board;
        Animator = new Animator<TAnimation>(go, animationPlayer);
    }
}