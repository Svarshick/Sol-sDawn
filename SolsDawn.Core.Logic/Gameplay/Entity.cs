using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;

namespace SolsDawn.Core.Logic.Gameplay;

public class EntityStats
{
    public Color Color;
    public float Width;
    public float Height;
}

public class Entity : Component
{
    public Transform2 Transform => GameObject.Transform;
    
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

        state.Enter(State);
        State = state;
        UpdateJob = state.Update();
    }
    
    public Entity(GameObject go) : base(go, true)
    {
        //HP = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
    }

    public void Kill() => Destroy();
}

public class Entity<TBoard, TAnimation> : Entity where TAnimation : AnimationPlayer
{ 
    public readonly Animator<TAnimation> Animator;
    public readonly TBoard Board;
    
    public Entity(GameObject go, TBoard board) : base(go)
    {
        Board = board;
        Animator = go.GetComponent<Animator<TAnimation>>() ?? throw new ComponentNotFoundException<Animator<TAnimation>>();
    }
}