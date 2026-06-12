namespace SolsDawn.Core.Logic.Configs;

public class LuaTimer : LuaEvent
{
    public double Delay { get; }
    public double TimeRemaining { get; set; }
    
    public LuaTimer(LuaRoutine owner, double delay) : base(owner)
    {
        Delay = delay;
        TimeRemaining = delay;
    }

    protected override void OnParentFired()
    {
        OwnerRoutine.StartTimer(this);
    }
}