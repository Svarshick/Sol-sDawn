using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Gameplay;

public delegate void HpChangedDelegate(int oldValue, int newValue);
    
public sealed class HP(
    GameObject go, 
    int max, 
    float invulnerabilityDuration = 0) 
    : Component<HP>(go)
{
    public int Max { get; } = max;

    public int Current
    {
        get;
        set
        {
            var oldValue = field;
            field = value;
            currentHpChanged?.Invoke(oldValue, value);
        }
    }

    public float InvulnerabilityDuration = invulnerabilityDuration;
    
    private double _lastHit;
    public event HpChangedDelegate currentHpChanged;
    public event HpChangedDelegate maxHpChanged;

    public override void Dispose()
    {
    }
    
    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => Time.TotalGameTime.TotalSeconds - _lastHit > InvulnerabilityDuration;
    public void UpdateInvulnerability() => _lastHit = Time.TotalGameTime.TotalSeconds;
}