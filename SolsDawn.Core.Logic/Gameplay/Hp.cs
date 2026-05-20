using System;

namespace SolsDawn.Core.Logic.Gameplay;

public sealed class Hp : Component<Hp>
{
    public int Max; 
    public float InvulnerabilityDuration;

    public int Current { get; private set; }

    private double _lastHit;
    
    public bool IsDied => Current <= 0;
    public bool IsInvulnerable => Time.TotalGameTime.TotalSeconds - _lastHit > InvulnerabilityDuration;

    public Hp(GameObject go, int max) : base(go)
    {
        Max = max;
        Current = max;
    }

    public override void Dispose()
    {
    }

    public void Hit(int value)
    {
        Current = Math.Max(0, Current - value);
        _lastHit = Time.TotalGameTime.TotalSeconds;
    }
}