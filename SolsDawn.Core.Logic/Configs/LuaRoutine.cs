using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Configs;

public class LuaRoutine
{
    private readonly DynValue _coroutine;

    public object Actor { get; }
    public Script Script { get; }
    public string Package { get; }

    private LuaRoutine _blockRoutine;
    private List<LuaRoutine> _subroutines = new();
    private List<LuaRoutine> _subroutinesBuff = new();
    private List<LuaTimer> _activeTimers = new();
    private List<LuaTimer> _timersBuff = new();

    public bool IsDead { get; private set; } = false;

    public LuaRoutine(Script script, DynValue routine, object actorInstance, string package)
    {
        Script = script;
        _coroutine = script.CreateCoroutine(routine);
        Actor = actorInstance;
        Package = package;
    }

    public void StartTimer(LuaTimer timer) => _timersBuff.Add(timer);

    public void StartSubroutine(DynValue routine)
    {
        var subroutine = new LuaRoutine(Script, routine, Actor, Package);
        subroutine.Update();
        if (!subroutine.IsDead)
        {
            _subroutinesBuff.Add(subroutine);
        }
    }

    public void BlockWithRoutine(DynValue routine)
    {
        if (_blockRoutine is not null)
            throw new LogicException("[LUA ERROR] Can't block routine: block routine is already running");

        _blockRoutine = new LuaRoutine(Script, routine, Actor, Package);
        _blockRoutine.Update();
        _blockRoutine = _blockRoutine.IsDead ? null : _blockRoutine;
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
            Kill();
        }
    }

    private void Kill()
    {
        if (IsDead)
            return;

        using (LuaExecutionContext.Use(this))
        {
            IsDead = true;

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