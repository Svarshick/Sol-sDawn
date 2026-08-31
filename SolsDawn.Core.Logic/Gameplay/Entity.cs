using System;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract class State
{
    public virtual void Enter(State? from)
    {
    }

    public virtual Job Update() => Job.CompletedJob;

    public virtual void Exit(State? to)
    {
    }
}

public class Entity : Component
{
    public Transform2 Transform => GameObject.Transform;

    public readonly Job RootJob;
    public State? State { get; private set; }
    public Job? UpdateJob { get; private set; }
    
    public void Enter(State state)
    {
        if (State is not null)
        {
            State.Exit(state);
            if (UpdateJob is null)
            {
                Console.WriteLine($"[Warning] previous state {State} job is null");
            }
            else if (UpdateJob.IsEnded)
            {
                Console.WriteLine($"[Warning] previous state {State} job is ended");
            }
            else
            {
                UpdateJob.Kill();
            }
        }

        using (JobContext.Use(RootJob))
        {
            state.Enter(State);
            State = state;
            UpdateJob = state.Update();
        }
    }
    
    public Entity(GameObject go) : base(go, true)
    {
        RootJob = JobContext.CurrentJob ?? throw new LogicException($"Entity created out of Job");
        //HP = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
    }

    public void Kill() => Destroy();
}

public class Entity<TBoard, TAnimation> : Entity where TAnimation : AnimationPlayer
{ 
    public readonly Animator<TAnimation> Animator;
    public readonly TBoard Board;
    
    public Entity(GameObject go, TBoard board, TAnimation animationPlayer) : base(go)
    {
        Board = board;
        Animator = new Animator<TAnimation>(go, animationPlayer);
    }
}

public interface IHittable
{
    public uint MaxHP { get; set; }
    public void ChangeHP(int delta);
}