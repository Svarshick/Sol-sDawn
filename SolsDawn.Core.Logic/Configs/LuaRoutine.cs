using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Configs;

[MoonSharpUserData]
public class LuaRoutine
{
    //API
    public LuaTimer after(double time)
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

    [MoonSharpHidden] private readonly DynValue _coroutine;

    [MoonSharpHidden] public object Actor { get; }
    [MoonSharpHidden] public Script Script { get; }
    [MoonSharpHidden] public string Package { get; }

    [MoonSharpHidden] public LuaEvent FinishEvent { get; }

    [MoonSharpHidden] private LuaRoutine _blockRoutine;
    [MoonSharpHidden] private List<LuaRoutine> _subroutines = new();
    [MoonSharpHidden] private List<LuaRoutine> _subroutinesBuff = new();
    [MoonSharpHidden] private List<LuaTimer> _activeTimers = new();
    [MoonSharpHidden] private List<LuaTimer> _timersBuff = new();

    [MoonSharpHidden] public bool IsDead { get; private set; } = false;

    [MoonSharpHidden]
    public LuaRoutine(Script script, DynValue routine, object actorInstance, string package)
    {
        Script = script;
        _coroutine = script.CreateCoroutine(routine);
        Actor = actorInstance;
        Package = package;
        FinishEvent = new(this);
    }

    [MoonSharpHidden]
    public void StartTimer(LuaTimer timer) => _timersBuff.Add(timer);
    [MoonSharpHidden]
    public LuaRoutine CreateSubroutine(DynValue callback) => new(Script, callback, Actor, Package);

    [MoonSharpHidden]
    //TODO: check that routine isn't active (to not duplicate). Maybe make LuaRoutineState instead of only isDead
    public void StartSubroutine(LuaRoutine subroutine) 
    {
        subroutine.Update();
        if (!subroutine.IsDead)
        {
            _subroutinesBuff.Add(subroutine);
        }
    }

    [MoonSharpHidden]
    public void BlockWithRoutine(DynValue routine)
    {
        if (_blockRoutine is not null)
            throw new LogicException("[LUA ERROR] Can't block routine: block routine is already running");

        _blockRoutine = new LuaRoutine(Script, routine, Actor, Package);
        _blockRoutine.Update();
        _blockRoutine = _blockRoutine.IsDead ? null : _blockRoutine;
    }

    [MoonSharpHidden]
    public void Update()
    {
        if (IsDead)
            return;

        using (LuaExecutionContext.Use(this))
        {
            try
            {
                UpdateTimers();

                if (_blockRoutine is not null)
                {
                    _blockRoutine.Update();
                    _blockRoutine = _blockRoutine.IsDead ? null : _blockRoutine;
                }

                if (_blockRoutine is null)
                {
                    Resume();
                }

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

    [MoonSharpHidden]
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

    [MoonSharpHidden]
    private void Resume()
    {
        var result = _coroutine.Coroutine.Resume();
        if (_coroutine.Coroutine.State == CoroutineState.Dead)
        {
            FinishEvent.Fire();
            Kill();
        }
    }

    [MoonSharpHidden]
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
            _blockRoutine?.Kill();

            foreach (var subroutine in _subroutines)
            {
                subroutine.Kill();
            }

            foreach (var subroutine in _subroutinesBuff)
            {
                subroutine.Kill();
            }
        }
    }
}