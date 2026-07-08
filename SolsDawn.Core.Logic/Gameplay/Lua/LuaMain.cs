using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

public class LuaMain
{
    private readonly LuaRoutine _rootRoutine;
    private readonly List<LuaEvent> _eventsToFire;
    
    public LuaMain()
    {
        var loader = LuaExecutionContext.LuaLoader;
        var go = new GameObject();
        new HP(go, 100);
        var boss = new Entity(go);
        _rootRoutine = new LuaRoutine(loader.Script, loader.GetCompiledScript("init.lua"), boss);
         _eventsToFire = new();
    }

    public void EventToFire(LuaEvent evt)
    {
        _eventsToFire.Add(evt);
    }
    
    public void Update()
    {
        foreach (var evt in _eventsToFire)
        {
            evt.Fire();
        }
        _eventsToFire.Clear();
        
        _rootRoutine.Update();
    }
}