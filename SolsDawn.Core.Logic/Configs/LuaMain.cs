using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Configs;

public class LuaMain
{
    private readonly LuaRoutine _rootRoutine;
    private readonly List<LuaEvent> _eventsToFire;
    
    public LuaMain()
    {
        var manager = LuaExecutionContext.LuaLoader;
         _rootRoutine = new LuaRoutine(manager.BossScript, manager.GetCompiledScript("boss/boss_test.lua"), "BOSS_ACTOR", "boss");
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