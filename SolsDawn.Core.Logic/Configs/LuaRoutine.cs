using System;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Configs;

public class LuaRoutine
{
    private readonly DynValue _coroutine;
    private readonly object _actor;

    public bool IsDead { get; private set; } = false;

    public LuaRoutine(Script script, DynValue routine, object actorInstance)
    {
        _actor = actorInstance;
        _coroutine = script.CreateCoroutine(routine);
    }

    public void Update(bool allowResume)
    {
        if (!IsDead && allowResume)
        {
            Resume();
        }
    }

    private void Resume()
    {
        using (LuaExecutionContext.Use(_actor))
        {
            try
            {
                _coroutine.Coroutine.Resume();
                IsDead = IsDead || _coroutine.Coroutine.State == CoroutineState.Dead;
            }
            catch (InterpreterException ex)
            {
                Console.Error.WriteLine($"[LUA ERROR] {ex.DecoratedMessage}");
                IsDead = true;
            }
        }
    }
}