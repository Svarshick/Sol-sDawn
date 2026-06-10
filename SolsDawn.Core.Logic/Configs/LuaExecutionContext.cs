using System;

namespace SolsDawn.Core.Logic.Configs;

public static class LuaExecutionContext
{
    [field: ThreadStatic]
    public static object CurrentActor { get; private set; }

    public static IDisposable Use(object actor)
    {
        return new ContextScope(actor);
    }

    private class ContextScope : IDisposable
    {
        private readonly object _previous;

        public ContextScope(object actor)
        {
            _previous = CurrentActor;
            CurrentActor = actor;
        }

        public void Dispose()
        {
            CurrentActor = _previous;
        }
    }
}