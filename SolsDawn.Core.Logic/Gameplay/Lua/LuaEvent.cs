using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

public enum LuaEventState
{
    Pending,
    Fired,
    Canceled
}

[MoonSharpUserData]
public class LuaEvent
{
    //API
    public LuaTimer after(double time)
    {
        var t = new LuaTimer(OwnerRoutine, time);
        ChainEvent(t);
        return t;
    }
    
    public bool isFired => IsFired;
    public bool isCanceled => IsCanceled;
    public bool isEnded => IsEnded;
    
    public LuaRoutine onFire(DynValue callback) => OnFire(callback);
    public LuaRoutine onCancel(DynValue callback) => OnCancel(callback);
    public LuaRoutine onEnd(DynValue callback) => OnEnd(callback);
    
    
    //INTERNAL
    
    [MoonSharpHidden] public readonly LuaRoutine OwnerRoutine;

    [MoonSharpHidden] public LuaEventState State { get; protected set; }
    [MoonSharpHidden] public bool IsFired => State == LuaEventState.Fired;
    [MoonSharpHidden] public bool IsCanceled => State == LuaEventState.Canceled;
    [MoonSharpHidden] public bool IsEnded => State != LuaEventState.Pending;

    [MoonSharpHidden] private readonly List<LuaEvent> _nextEvents = new();
    [MoonSharpHidden] private readonly List<object> _fireCallbacks = new();
    [MoonSharpHidden] private readonly List<object> _cancelCallbacks = new();

    [MoonSharpHidden]
    public LuaEvent(LuaRoutine ownerRoutine)
    {
        OwnerRoutine = ownerRoutine;
    }

    [MoonSharpHidden]
    public void ChainEvent(LuaEvent nextEvent)
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

    [MoonSharpHidden]
    public LuaRoutine OnFire(object callback)
    {
        switch (callback)
        {
            case DynValue routineCallback:
            {
                var routine = OwnerRoutine.CreateSubroutine(routineCallback);
                switch (State)
                {
                    case LuaEventState.Fired:
                        OwnerRoutine.StartSubroutine(routine);
                        break;
                    case LuaEventState.Pending:
                        _fireCallbacks.Add(routine);
                        break;
                    default:
                        routine.Kill();
                        break;
                }

                return routine;
            }

            case Action actionCallback:
            {
                switch (State)
                {
                    case LuaEventState.Fired:
                        actionCallback();
                        break;
                    case LuaEventState.Pending:
                        _fireCallbacks.Add(actionCallback);
                        break;
                }

                return null;
            }

            default:
            {
                throw new NotImplementedException($"Can't work with {callback.GetType()} type");
            }
        }
    }

    [MoonSharpHidden]
    public LuaRoutine OnCancel(object callback)
    {
        switch (callback)
        {
            case DynValue routineCallback:
            {
                var routine = OwnerRoutine.CreateSubroutine(routineCallback);
                switch (State)
                {
                    case LuaEventState.Canceled:
                        OwnerRoutine.StartSubroutine(routine);
                        break;
                    case LuaEventState.Pending:
                        _cancelCallbacks.Add(routine);
                        break;
                    default:
                        routine.Kill();
                        break;
                }

                return routine;
            }

            case Action actionCallback:
            {
                switch (State)
                {
                    case LuaEventState.Canceled:
                        actionCallback();
                        break;
                    case LuaEventState.Pending:
                        _cancelCallbacks.Add(actionCallback);
                        break;
                }

                return null;
            }

            default:
            {
                throw new NotImplementedException($"Can't work with {callback.GetType()} type");
            }
        }
    }

    [MoonSharpHidden]
    public LuaRoutine OnEnd(object callback)
    {
        switch (callback)
        {
            case DynValue routineCallback:
            {
                var routine = OwnerRoutine.CreateSubroutine(routineCallback);
                switch (State)
                {
                    case LuaEventState.Fired:
                    case LuaEventState.Canceled:
                        OwnerRoutine.StartSubroutine(routine);
                        break;
                    case LuaEventState.Pending:
                        _fireCallbacks.Add(routine);
                        _cancelCallbacks.Add(routine);
                        break;
                }

                return routine;
            }

            case Action actionCallback:
            {
                switch (State)
                {
                    case LuaEventState.Fired:
                    case LuaEventState.Canceled:
                        actionCallback();
                        break;
                    case LuaEventState.Pending:
                        _fireCallbacks.Add(actionCallback);
                        _cancelCallbacks.Add(actionCallback);
                        break;
                }

                return null;
            }

            default:
            {
                throw new NotImplementedException($"Can't work with {callback.GetType()} type");
            }
        }
    }

    [MoonSharpHidden]
    protected virtual void OnParentFired() => Fire();

    [MoonSharpHidden]
    protected virtual void OnParentCanceled() => Cancel();

    [MoonSharpHidden]
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

    [MoonSharpHidden]
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

    [MoonSharpHidden]
    private void InvokeCallback(object callback)
    {
        switch (callback)
        {
            case LuaRoutine routine:
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