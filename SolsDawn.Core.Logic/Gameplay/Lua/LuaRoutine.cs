using System;
using System.Collections.Generic;
using Lua;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

[LuaObject]
public class LuaRoutine
{
    [LuaMember("after")]
    public LuaTimer After(double time)
    {
        var t = new LuaTimer(this, time);
        FinishEvent.ChainEvent(t); 
        return t;
    }

    public LuaRoutine onFinish(DynValue callback) => FinishEvent.OnFire(callback);
    public LuaRoutine onCancel(DynValue callback) => FinishEvent.OnCancel(callback);
    public LuaRoutine onEnd(DynValue callback)    => FinishEvent.OnEnd(callback);

    public void kill() => Kill();

    public LuaEvent finished => FinishEvent;  

    
    //INTERNAL

    public Script Script { get; }
    public LuaEvent FinishEvent { get; }
    public bool IsDead { get; private set; } = false;

    private readonly DynValue _coroutine;
    private List<LuaRoutine> _subroutines = new();
    private List<LuaRoutine> _subroutinesBuff = new();
    private List<LuaTimer> _activeTimers = new();
    private List<LuaTimer> _timersBuff = new();

    private readonly List<IDisposable> _disposables;

    public LuaRoutine(Script script, DynValue routine)
    {
        Script = script;
        _coroutine = script.CreateCoroutine(routine);
        FinishEvent = new(this);
    }

    public void StartTimer(LuaTimer timer) => _timersBuff.Add(timer);
    public LuaRoutine CreateSubroutine(DynValue callback) => new(Script, callback);

    //TODO: check that routine isn't active (to not duplicate). Maybe make LuaRoutineState instead of only isDead
    public void StartSubroutine(LuaRoutine subroutine) 
    {
        subroutine.Update();
        if (!subroutine.IsDead)
        {
            _subroutinesBuff.Add(subroutine);
        }
    }

    public void AddResource(IDisposable resource)
    {
        _disposables.Add(resource);
    }
    
    public void Update()
    {
        if (IsDead)
            return;

        using (LuaExecutionContext.Use(this))
        {
            try
            {
                UpdateTimers();
                Resume();

                foreach (var subroutine in _subroutines)
                {
                    subroutine.Update();
                    if (!subroutine.IsDead)
                    {
                        _subroutinesBuff.Add(subroutine);
                    }
                }

                (_subroutines, _subroutinesBuff) = (_subroutinesBuff, _subroutines);
                _subroutinesBuff.Clear();
            }
            catch (InterpreterException ex)
            {
                Console.Error.WriteLine($"[LUA ERROR] {ex.DecoratedMessage}");
                Kill();
            }
        }
    }

    private void UpdateTimers()
    {
        foreach (var timer in _activeTimers)
        {
            if (timer.State != LuaEventState.Pending)
                continue;

            timer.TimeRemaining -= Time.ElapsedGameTime.TotalSeconds;
            if (timer.TimeRemaining <= 0)
            {
                timer.Fire();
            }
            else
            {
                _timersBuff.Add(timer);
            }
        }

        (_activeTimers, _timersBuff) = (_timersBuff, _activeTimers);
        _timersBuff.Clear();
    }

    private void Resume()
    {
        var result = _coroutine.Coroutine.Resume();
        if (_coroutine.Coroutine.State == CoroutineState.Dead)
        {
            FinishEvent.Fire();
            Kill();
        }
    }

    public void Kill()
    {
        if (IsDead)
            return;

        using (LuaExecutionContext.Use(this))
        {
            IsDead = true;
            FinishEvent.Cancel();

            //CANCEL TIMERS
            foreach (var timer in _activeTimers)
            {
                timer.Cancel();
            }

            foreach (var timer in _timersBuff)
            {
                timer.Cancel();
            }

            //KILL ROUTINES
            foreach (var subroutine in _subroutines)
            {
                subroutine.Kill();
            }

            foreach (var subroutine in _subroutinesBuff)
            {
                subroutine.Kill();
            }

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
        }
    }
}