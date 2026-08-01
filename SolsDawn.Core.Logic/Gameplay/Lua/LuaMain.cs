using System.Collections.Generic;
using System.IO;
using SolsDawn.Core.Logic.Gameplay.Behaviour;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

public class LuaMain
{
    private readonly LuaRoutine _rootRoutine;
    private readonly List<LuaEvent> _eventsToFire;
    
    public LuaMain(string root, Input input)
    {
        var loader = new LuaAPI(root, input);
        var absolutePath = Path.Combine(root, "init.lua");
        var code = File.ReadAllText(absolutePath);
        var initFunction = loader.Script.LoadString(code, loader.API, absolutePath);
        _rootRoutine = new LuaRoutine(loader.Script, initFunction);
        
        _eventsToFire = new();
        
        var go = new GameObject();
        new HP(go, 100);
        var boss = new Entity(go);
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