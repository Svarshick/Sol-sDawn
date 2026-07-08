using System;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

public static class LuaExecutionContext
{
    [field: ThreadStatic]
    public static LuaRoutine CurrentRoutine { get; private set; }

    public static LuaLoader LuaLoader { get; } = new ("Configs/Lua");

    public static IDisposable Use(LuaRoutine routine)
    {
        return new ContextScope(routine);
    }

    private class ContextScope : IDisposable
    {
        private readonly LuaRoutine _previous;

        public ContextScope(LuaRoutine routine)
        {
            _previous = CurrentRoutine;
            CurrentRoutine = routine;
        }

        public void Dispose()
        {
            CurrentRoutine = _previous;
        }
    }
}