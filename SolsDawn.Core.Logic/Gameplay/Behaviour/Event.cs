using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public enum EventState
{
    Pending,
    Fired,
    Canceled
}

public readonly struct EventAwaiter : INotifyCompletion
{
    private readonly Event _event;
    private readonly Routine _routine;
    
    public EventAwaiter(Event @event, Routine routine)
    {
        _event = @event;
        _routine = routine;
    }
    
    public bool IsCompleted => _event.IsEnded;
    
    public void OnCompleted(Action continuation)
    {
        var routine = _routine;
        _event.OnEnd(() =>
        {
            using (ExecutionContext.Use(routine))
            {
                continuation();
            }
        });
    }

    public void GetResult()
    {
    }
}


public class Timer : Event
{
    public double Delay { get; }
    public double TimeRemaining { get; set; }
    
    public Timer(Routine owner, double delay) : base(owner)
    {
        Delay = delay;
        TimeRemaining = delay;
    }

    protected override void OnParentFired()
    {
        OwnerRoutine.StartTimer(this);
    }
}

public class Event
{
    public readonly Routine OwnerRoutine;

    public EventState State { get; protected set; }
    public bool IsFired => State == EventState.Fired;
    public bool IsCanceled => State == EventState.Canceled;
    public bool IsEnded => State != EventState.Pending;

    private readonly List<Event> _nextEvents = new();
    private readonly List<object> _fireCallbacks = new();
    private readonly List<object> _cancelCallbacks = new();

    public Event(Routine ownerRoutine)
    {
        OwnerRoutine = ownerRoutine;
    }
    
    public EventAwaiter GetAwaiter()
    {
        return new EventAwaiter(this, ExecutionContext.CurrentRoutine);
    }
    
    public Timer After(double time)
    {
        var t = new Timer(OwnerRoutine, time);
        ChainEvent(t);
        return t;
    }

    public void ChainEvent(Event nextEvent)
    {
        switch (State)
        {
            case EventState.Pending:
                _nextEvents.Add(nextEvent);
                break;
            case EventState.Fired:
                nextEvent.OnParentFired();
                break;
            case EventState.Canceled:
                nextEvent.OnParentCanceled();
                break;
        }
    }

    public Routine OnFire(Callback callback)
    {
        var routine = new Routine(callback);
        switch (State)
        {
            case EventState.Fired:
                OwnerRoutine.StartSubroutine(routine);
                break;
            case EventState.Pending:
                _fireCallbacks.Add(routine);
                break;
            default:
                routine.Kill();
                break;
        }

        return routine;
    }

    public void OnFire(Action callback)
    {
        switch (State)
        {
            case EventState.Fired:
                callback();
                break;
            case EventState.Pending:
                _fireCallbacks.Add(callback);
                break;
        }
    }

    public Routine OnCancel(Callback callback)
    {
        var routine = new Routine(callback);
        switch (State)
        {
            case EventState.Canceled:
                OwnerRoutine.StartSubroutine(routine);
                break;
            case EventState.Pending:
                _cancelCallbacks.Add(routine);
                break;
            default:
                routine.Kill();
                break;
        }

        return routine;
    }

    public void OnCancel(Action callback)
    {
        switch (State)
        {
            case EventState.Canceled:
                callback();
                break;
            case EventState.Pending:
                _cancelCallbacks.Add(callback);
                break;
        }
    }

    public Routine OnEnd(Callback callback)
    {
        var routine = new Routine(callback);
        switch (State)
        {
            case EventState.Fired:
            case EventState.Canceled:
                OwnerRoutine.StartSubroutine(routine);
                break;
            case EventState.Pending:
                _fireCallbacks.Add(routine);
                _cancelCallbacks.Add(routine);
                break;
        }

        return routine;
    }

    public void OnEnd(Action callback)
    {
        switch (State)
        {
            case EventState.Fired:
            case EventState.Canceled:
                callback();
                break;
            case EventState.Pending:
                _fireCallbacks.Add(callback);
                _cancelCallbacks.Add(callback);
                break;
        }
    }

    protected virtual void OnParentFired() => Fire();

    protected virtual void OnParentCanceled() => Cancel();

    public void Fire()
    {
        if (State != EventState.Pending)
            return;

        State = EventState.Fired;

        using (ExecutionContext.Use(OwnerRoutine))
        {
            foreach (var callback in _fireCallbacks)
            {
                InvokeCallback(callback);
            }

            foreach (var next in _nextEvents)
            {
                next.OnParentFired();
            }
        }

        _fireCallbacks.Clear();
        _cancelCallbacks.Clear();
        _nextEvents.Clear();
    }

    public void Cancel()
    {
        if (State != EventState.Pending)
            return;

        State = EventState.Canceled;
        using (ExecutionContext.Use(OwnerRoutine))
        {
            foreach (var callback in _cancelCallbacks)
            {
                InvokeCallback(callback);
            }

            foreach (var next in _nextEvents)
            {
                next.OnParentCanceled();
            }
        }

        _fireCallbacks.Clear();
        _cancelCallbacks.Clear();
        _nextEvents.Clear();
    }

    private void InvokeCallback(object callback)
    {
        switch (callback)
        {
            case Routine routine:
                OwnerRoutine.StartSubroutine(routine);
                break;
            case Action action:
                action();
                break;
            default:
                throw new NotImplementedException($"Can't work with {callback.GetType()} type");
        }
    }
}