using MonoGame.Extended;
using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Animations;

public enum AnimationState
{
    Active,
    Paused,
    Finished,
    Killed
}

public abstract class Animation : IDrawable, IUpdatable
{
    public Transform2 Transform { get; } = new();
    public AnimationState State { get; protected set; }
    public bool IsCycled { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public double TimeToKill { get; set; } = -1;

    public void Pause()
    {
        if (State == AnimationState.Active)
            State = AnimationState.Paused;
    }

    public void Resume()
    {
        if (State == AnimationState.Paused)
            State = AnimationState.Active;
    }

    public void Kill() => State = AnimationState.Killed;

    public void Reset()
    {
        switch (State)
        {
            case AnimationState.Active:
            case AnimationState.Paused:
                OnReset();
                break;
            case AnimationState.Finished:
                OnReset();
                State = AnimationState.Active;
                break;
        }
    }

    protected virtual void OnReset()
    {
    }

    public virtual void Draw()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {

    }
}

public class AnimationsPool : IUpdatable, IDrawable
{
    private List<Animation> _animations = new();
    private List<Animation> _animationsBuff = new();

    public void Add(Animation animation) => _animationsBuff.Add(animation);
    

    public void Update()
    {
        _animations.AddRange(_animationsBuff);
        _animationsBuff.Clear();

        foreach (var animation in _animations)
        {
            if (animation.TimeToKill != -1)
            {
                animation.TimeToKill -= Time.ElapsedGameTime.TotalSeconds;
                if (animation.TimeToKill <= 0)
                {
                    animation.Kill();
                }
            }
            
            if (animation.State == AnimationState.Active ||
                animation.State == AnimationState.Finished && animation.IsCycled)
            {
                animation.Update();
                _animationsBuff.Add(animation);
            }
            else if (animation.State == AnimationState.Paused)
            {
                _animationsBuff.Add(animation);
            }
        }
        
        (_animations, _animationsBuff) = (_animationsBuff, _animations);
        _animationsBuff.Clear();
    }
    
    public void LateUpdate()
    {
        foreach (var animation in _animations)
        {
            if (animation.State == AnimationState.Active ||
                animation.State == AnimationState.Finished && animation.IsCycled)
            {
                animation.LateUpdate();
                _animationsBuff.Add(animation);
            }
            else if (animation.State == AnimationState.Paused)
            {
                _animationsBuff.Add(animation);
            }
        }
        
        (_animations, _animationsBuff) = (_animationsBuff, _animations);
        _animationsBuff.Clear();
    }

    public void Draw()
    {
        foreach (var animation in _animations)
        {
            if (animation.IsVisible)
            {
                animation.Draw();
            }
        }
    }
}