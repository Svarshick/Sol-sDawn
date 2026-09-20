using System;
using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public enum EventStreamState
{ 
    Idle,
    Firing,
    Canceled
}

public sealed class EventStream
{
    public readonly Job Owner;
    public bool IsActive => _state != EventStreamState.Canceled;
    
    private int _delayedFire = 0;
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

        if (_state == EventStreamState.Firing)
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

        if (_state == EventStreamState.Firing)
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
    //Event returns Job for JobMethod. But EventStream Fires several times and creates several JobMethods.
    //I decided it to return void instead of Job
    //If Job tracking required, I will add "JobSource" class, that contains field for Job and updated every Fire
    
    public void OnFire(Action action)
    {
        if (!IsActive)
            return;

        if (_state == EventStreamState.Firing)
        {
            _buff.Add(action);
        }
        else
        {
            _fireCallbacks.Add(action);
        }
    }

    public void OnFire(JobMethod method)
    {
        if (!IsActive)
            return;

        if (_state == EventStreamState.Firing)
        {
            _buff.Add(method);
        }
        else
        {
            _fireCallbacks.Add(method);
        }
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

    public void OnCancel(JobMethod method)
    {
        if (!IsActive)
        {
            using (JobContext.Use(Owner))
            {
                method();
            }
        }
        else
        {
            _cancelCallbacks.Add(method);
        }
    }

    public void OnNext(Action action)
    {
        if (!IsActive)
        {
            action();
            return;
        }

        if (_state == EventStreamState.Firing)
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
        
    public void OnNext(JobMethod method)
    {
        if (!IsActive)
        {
            using (JobContext.Use(Owner))
            {
                method();
            }

            return;
        }

        if (_state == EventStreamState.Firing)
        {
            _buff.Add(method);
            _cancelCallbacks.Add(method);
        }
        else
        {
            _fireCallbacks.Add(method);
            _cancelCallbacks.Add(method);
        }
    }

    #endregion

    #region Fire&Cancel

    public void Fire()
    {
        if (!IsActive)
            return;

        _delayedFire++;
        if (_delayedFire > 1)
            return;

        _state = EventStreamState.Firing;
        while (_delayedFire > 0)
        {
            using (JobContext.Use(Owner))
            {
                for (int i = 0; i < _fireCallbacks.Count; i++)
                {
                    InvokeCallback(_fireCallbacks[i]);
                    if (!IsActive)
                        return;
                }

                for (int i = 0; i < _nextEvents.Count; i++)
                {
                    _nextEvents[i].Fire();
                    if (!IsActive)
                        return;
                }

                int streamOffset = 0;
                for (int i = 0; i + streamOffset < _nextStreams.Count;)
                {
                    var stream = _nextStreams[i + streamOffset];
                    if (!stream.IsActive)
                    {
                        streamOffset++;
                    }
                    else
                    {
                        stream.Fire();
                        if (!IsActive)
                            return;
                        _nextStreams[i] = stream;
                        i++;
                    }
                }
                _nextStreams.RemoveRange(_nextStreams.Count - streamOffset, streamOffset);

                _nextEvents.Clear();
                foreach (var e in _buff)
                {
                    switch (e)
                    {
                        case JobMethod method:
                            _fireCallbacks.Add(method);
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

            _delayedFire--;
        }

        _state = EventStreamState.Idle;
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

            foreach (var e in _buff)
            {
                switch (e)
                {
                    case Event @event:
                        _nextEvents.Add(@event);
                        break;
                    case EventStream stream:
                        _nextStreams.Add(stream);
                        break;
                }
            }

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
            case JobMethod method:
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
}