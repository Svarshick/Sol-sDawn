using System;
using System.Collections.Generic;

//need to add Event<T>. It works in different way (try to look at gemini chat)
//Event<T> is not Event inheritor (because we can't use Event<T> as Event, e.g. Fire without argument T)
//Event<T> can't be "just chained" by Event<D>:
//Event -> Event OK, no args
//Event -> Event<T> NOT OK, args required. We can chain Event<T> by providing Func<T> that allows evt.Fire(func())
//Event -> Event OK, no args
//Event<T> -> Event<T> OK, args the same
//Event<T> -> Event<D> NOT OK, args required. We can chain Event<D> by providing Func<T, D> that allows evt.Fire(func(t))
//similar with EventStream<T>
//race just contains object[] and winner is private field (API already allows to control winner by OnWinner)

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public enum EventState
{
    Pending,
    Fired,
    Canceled
}

public delegate Job JobMethod();

public class Timer : Event
{
    public double Delay { get; }
    public double TimeRemaining { get; internal set; }
    
    public Timer(Job owner, double delay) : base(owner)
    {
        Delay = delay;
        TimeRemaining = delay;
    }

    internal override void OnParentFired()
    {
        Owner.StartTimer(this);
    }
}

public class Event
{
    public readonly Job Owner;

    public EventState State { get; protected set; }
    public bool IsFired => State == EventState.Fired;
    public bool IsCanceled => State == EventState.Canceled;
    public bool IsEnded => State != EventState.Pending;

    private readonly List<Event> _nextEvents = new();
    private readonly List<object> _fireCallbacks = new();
    private readonly List<object> _cancelCallbacks = new();
    
    private readonly List<object> _buff = new(); //todo

    public Event(Job owner)
    {
        Owner = owner;
    }
    
    public EventAwaiter GetAwaiter()
    {
        if (!AwaiterUtils.ValidContext())
            throw new JobException();
        return new EventAwaiter(JobContext.CurrentJob!, this);
    }
    
    public Timer After(double time)
    {
        var t = new Timer(Owner, time);
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

    public Job OnFire(JobMethod method)
    {
        var job = new Job(); 
        switch (State)
        {
            case EventState.Fired:
                using (JobContext.Use(Owner))
                {
                    JobAsyncMethodBuilder.PreallocatedJob = job;
                    method();
                }
                break;
            case EventState.Pending:
                _fireCallbacks.Add((job, method));
                break;
            case EventState.Canceled:
                job.Kill();
                break;
        }

        return job;
    }

    public void OnFire(Action action)
    {
        switch (State)
        {
            case EventState.Fired:
                action();
                break;
            case EventState.Pending:
                _fireCallbacks.Add(action);
                break;
        }
    }

    public Job OnCancel(JobMethod method)
    {
        var job = new Job();
        switch (State)
        {
            case EventState.Canceled:
                using (JobContext.Use(Owner))
                {
                    JobAsyncMethodBuilder.PreallocatedJob = job;
                    method();
                }
                break;
            case EventState.Pending:
                _cancelCallbacks.Add((job, method));
                break;
            case EventState.Fired:
                job.Kill();
                break;
        }

        return job;
    }

    public void OnCancel(Action action)
    {
        switch (State)
        {
            case EventState.Canceled:
                action();
                break;
            case EventState.Pending:
                _cancelCallbacks.Add(action);
                break;
        }
    }

    public Job OnEnd(JobMethod method)
    {
        var job = new Job();
        switch (State)
        {
            case EventState.Fired:
            case EventState.Canceled:
                using (JobContext.Use(Owner))
                {
                    JobAsyncMethodBuilder.PreallocatedJob = job;
                    method();
                }
                break;
            case EventState.Pending:
                _fireCallbacks.Add((job, method));
                _cancelCallbacks.Add((job, method));
                break;
        }

        return job;
    }

    public void OnEnd(Action action)
    {
        switch (State)
        {
            case EventState.Fired:
            case EventState.Canceled:
                action();
                break;
            case EventState.Pending:
                _fireCallbacks.Add(action);
                _cancelCallbacks.Add(action);
                break;
        }
    }

    internal virtual void OnParentFired() => Fire();

    internal virtual void OnParentCanceled() => Cancel();

    public void Fire()
    {
        if (IsEnded)
            return;

        State = EventState.Fired;

        using (JobContext.Use(Owner))
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
        if (IsEnded)
            return;

        State = EventState.Canceled;
        using (JobContext.Use(Owner))
        {
            foreach (var callback in _cancelCallbacks)
                InvokeCallback(callback);

            foreach (var next in _nextEvents)
                next.OnParentCanceled();
        }

        _fireCallbacks.Clear();
        _cancelCallbacks.Clear();
        _nextEvents.Clear();
    }

    private void InvokeCallback(object callback)
    {
        switch (callback)
        {
            case (Job job, JobMethod method):
                JobAsyncMethodBuilder.PreallocatedJob = job;
                method();
                break;
            case Action action:
                action();
                break;
            default:
                throw new NotImplementedException($"Can't work with {callback.GetType()} type");
        }
    }
}