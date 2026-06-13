using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Configs;

public enum LuaEventState
{
    Pending,
    Fired,
    Canceled
}

public class LuaEventProxy
{
    private LuaEvent _target;

    [MoonSharpHidden]
    public LuaEventProxy(LuaEvent target)
    {
        _target = target;
    }

    public LuaTimer after(double time)
    {
        var t = new LuaTimer(_target.OwnerRoutine, time);
        _target.Then(t);
        return t;
    }

    public bool isFired => _target.IsFired;
    public bool isCanceled => _target.IsCanceled;
    public int id => _target.ID;
}

public class LuaEvent
{
    public readonly LuaRoutine OwnerRoutine;
    
    private static int _id_counter = 0;
    public int ID { get; } = _id_counter++;

    
    public LuaEventState State { get; protected set; }
    public bool IsFired => State == LuaEventState.Fired;
    public bool IsCanceled => State == LuaEventState.Canceled;

    private readonly List<LuaEvent> _nextEvents = new();
    private readonly List<object> _fireCallbacks = new();
    private readonly List<object> _cancelCallbacks = new();

    public LuaEvent(LuaRoutine ownerRoutine)
    {
        OwnerRoutine = ownerRoutine;
    }

    public void Then(LuaEvent nextEvent)
    {
        switch (State)
        {
            case LuaEventState.Pending:
                _nextEvents.Add(nextEvent);
                break;
            case LuaEventState.Fired:
                nextEvent.OnParentFired();
                break;
            case LuaEventState.Canceled:
                nextEvent.OnParentCanceled();
                break;
        }
    }

    public void OnFire(object callback)
    {
        switch (State)
        {
            case LuaEventState.Fired:
                InvokeCallback(callback);
                break;
            case LuaEventState.Pending:
                _fireCallbacks.Add(callback);
                break;
        }
    }

    public void OnCancel(object callback) 
    {
        switch (State)
        {
            case LuaEventState.Canceled:
                InvokeCallback(callback);
                break;
            case LuaEventState.Pending:
                _cancelCallbacks.Add(callback);
                break;
        }
    }

    protected virtual void OnParentFired() => Fire();
    protected virtual void OnParentCanceled() => Cancel();
    
    public void Fire()
    {
        if (State != LuaEventState.Pending)
            return;

        State = LuaEventState.Fired;
        foreach (var callback in _fireCallbacks)
        {
            InvokeCallback(callback);
        }
        
        foreach (var next in _nextEvents)
        {
            next.OnParentFired();
        }
        
        _fireCallbacks.Clear();
        _cancelCallbacks.Clear();
        _nextEvents.Clear();
    }

    public void Cancel()
    {
        if (State != LuaEventState.Pending)
            return;
        
        State = LuaEventState.Canceled;
        foreach (var callback in _cancelCallbacks)
        {
            InvokeCallback(callback);
        }

        foreach (var next in _nextEvents)
        {
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
            case Action action:
                action();
                break;
            case DynValue dynValue:
                OwnerRoutine.StartSubroutine(dynValue);
                break;
        }
    }
}