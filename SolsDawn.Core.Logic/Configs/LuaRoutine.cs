using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Configs;

public class LuaRoutine
{
    private readonly DynValue _coroutine;
    
    public object Actor { get; }
    public Script Script { get; }
    
    private List<LuaRoutine> _subroutines = new();
    private List<LuaRoutine> _subroutinesBuff = new();
    private List<LuaTimer> _activeTimers = new();
    private List<LuaTimer> _timersBuff = new();

    public bool IsDead { get; private set; } = false;

    public LuaRoutine(Script script, DynValue routine, object actorInstance)
    {
        Script = script;
        _coroutine = script.CreateCoroutine(routine);
        Actor = actorInstance;
    }

    public void StartTimer(LuaTimer timer) => _activeTimers.Add(timer);

    public void StartSubroutine(DynValue routine)
    {
        var subroutine = new LuaRoutine(Script, routine, Actor);
        subroutine.Update();
        if (!subroutine.IsDead)
        {
            _subroutinesBuff.Add(subroutine);
        }
    }

    public void Update()
    {
        if (IsDead)
            return;

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
        using (LuaExecutionContext.Use(this))
        {
            try
            {
                var result = _coroutine.Coroutine.Resume();
                if (_coroutine.Coroutine.State == CoroutineState.Dead)
                {
                    Kill();
                }
            }
            catch (InterpreterException ex)
            {
                Console.Error.WriteLine($"[LUA ERROR] {ex.DecoratedMessage}");
                Kill();
            }
        }
    }

    private void Kill()
    {
        if (IsDead)
            return;
        
        IsDead = true;
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