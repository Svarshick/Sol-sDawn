//DEVELOPING

/*using System;
using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public enum EventStreamState
{
    Idle,
    FiringCallbacks,
    FiringEvents,
    FiringStreams,
    Canceled
}

public sealed class EventStream
{
    public readonly Job Owner;
    public bool IsActive => _state != EventStreamState.Canceled;
    
    private int _delayedSignals = 0;
    private EventStreamState _state;
        
    private readonly List<object> _fireCallbacks = new();
    private readonly List<Event> _nextEvents = new();
    private readonly List<EventStream> _nextStreams = new();
    private readonly List<object> _cancelCallbacks = new();
    
    private readonly List<object> _buff = new();

    public EventStream(Job owner)
    {
        Owner = owner;
    }
    
    #region Chains
    
    public void ChainEvent(Event nextEvent)
    {
        if (!IsActive)
        {
            nextEvent.OnParentCanceled();
            return;
        }

        if (_state == EventStreamState.FiringEvents)
        {
            _buff.Add(nextEvent);
        }
        else
        {
            _nextEvents.Add(nextEvent);
        }
    }

    public void ChainStream(EventStream stream)
    {
        if (!IsActive)
        {
            stream.Cancel();
            return;
        }

        if (_state == EventStreamState.FiringStreams)
        {
            _buff.Add(stream);
        }
        else
        {
            _nextStreams.Add(stream);
        }
    }
    
    #endregion

    #region On{Fire,Cancel,Next}
    
    public void OnFire(Action action)
    {
        if (!IsActive)
            return;

        if (_state == EventStreamState.FiringCallbacks)
        {
            _buff.Add(action);
        }
        else
        {
            _fireCallbacks.Add(action);
        }
    }

    public Job OnFire(JobMethod method)
    {
        var job = new Job();
        if (!IsActive)
        {
            job.Kill();
            return job;
        }

        if (_state == EventStreamState.FiringCallbacks)
        {
            _buff.Add((job, method));
        }
        else
        {
            _fireCallbacks.Add((job, method));
        }
        
        return job;
    }

    public void OnCancel(Action action)
    {
        if (!IsActive)
        {
            action();
        }
        else
        {
            _cancelCallbacks.Add(action);
        }
    }

    public Job OnCancel(JobMethod method)
    {
        var job = new Job();
        if (!IsActive)
        {
            using (JobContext.Use(Owner))
            {
                JobAsyncMethodBuilder.PreallocatedJob = job;
                method();
            }
        }
        else
        {
            _cancelCallbacks.Add((job, method));
        }

        return job;
    }

    public void OnNext(Action action)
    {
        if (!IsActive)
        {
            action();
            return;
        }

        if (_state == EventStreamState.FiringCallbacks)
        {
            _buff.Add(action);
            _cancelCallbacks.Add(action);
        }
        else
        {
            _fireCallbacks.Add(action);
            _cancelCallbacks.Add(action);
        }
    }
        
    public Job OnNext(JobMethod method)
    {
        var job = new Job();
        if (!IsActive)
        {
            using (JobContext.Use(Owner))
            {
                JobAsyncMethodBuilder.PreallocatedJob = job;
                method();
            }
            return job;
        }

        if (_state == EventStreamState.FiringCallbacks)
        {
            _buff.Add((job, method));
            _cancelCallbacks.Add((job, method));
        }
        else
        {
            _fireCallbacks.Add((job, method));
            _cancelCallbacks.Add((job, method));
        }
        
        return job;
    }

    #endregion

    #region Fire&Cancel
    
    public void Fire()
    {
        if (!IsActive)
            return;

        using (JobContext.Use(Owner))
        {
            _state = EventStreamState.FiringCallbacks;
            for (int i = 0; i < _fireCallbacks.Count; i++)
            {
                InvokeCallback(_fireCallbacks[i]);
                if (!IsActive)
                    return;
            }

            _state = EventStreamState.FiringEvents;
            for (int i = 0; i < _nextEvents.Count; i++)
            {
                _nextEvents[i].Fire();
                if (!IsActive)
                    return;
            }

            _state = EventStreamState.FiringStreams;
            for (int i = 0; i < _nextStreams.Count; i++) //add clearing form canceled streams (like job and timers/subjobs)
            {
                _nextStreams[i].Fire();
                if (!IsActive)
                    return;
            }

            _state = EventStreamState.Idle;
            _nextEvents.Clear();
            foreach (var e in _buff)
            {
                switch (e)
                {
                    case (Job job, JobMethod method):
                        _fireCallbacks.Add((job, method));
                        break;
                    case Action action:
                        _fireCallbacks.Add(action);
                        break;
                    case Event @event:
                        _nextEvents.Add(@event);
                        break;
                    case EventStream stream:
                        _nextStreams.Add(stream);
                        break;
                    default:
                        throw new NotImplementedException($"Can't work with {e.GetType()} type");
                }
            }

            _buff.Clear();
        }
    }

    public void Cancel()
    {
        if (!IsActive)
            return;

        _state = EventStreamState.Canceled;
        using (JobContext.Use(Owner))
        {
            foreach (var callback in _cancelCallbacks)
                InvokeCallback(callback);

            foreach (var @event in _nextEvents)
                @event.Cancel();

            foreach (var stream in _nextStreams)
                stream.Cancel();
        }

        _fireCallbacks.Clear();
        _nextEvents.Clear();
        _nextStreams.Clear();
        _buff.Clear();
        _cancelCallbacks.Clear();
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
    
    #endregion
}*/