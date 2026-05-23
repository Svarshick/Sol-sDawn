namespace SolsDawn.Core.Logic.Gameplay;

public delegate void HpChangedDelegate(int oldValue, int newValue);
    
public sealed class Hp(
    GameObject go, 
    int max, 
    float invulnerabilityDuration = 0) 
    : Component<Hp>(go)
{
    public int Max;
    public int Current; 

    public float InvulnerabilityDuration = invulnerabilityDuration;
    private double _lastHit;
    public event HpChangedDelegate currentHpChanged;
    public event HpChangedDelegate maxHpChanged;

    public override void Dispose()
    {
    }
    
    public bool IsDied => Current <= 0;
    public bool IsInvulnerable => Time.TotalGameTime.TotalSeconds - _lastHit > InvulnerabilityDuration;
    public void UpdateInvulnerability() => _lastHit = Time.TotalGameTime.TotalSeconds;

    public void ChangeCurrent(int newValue)
    {
        var oldValue = Current;
        Current = newValue;
        currentHpChanged?.Invoke(oldValue, newValue);
    }

    public void ChangeMax(int newValue)
    {
        var oldValue = Current;
        Current = newValue;
        maxHpChanged?.Invoke(oldValue, newValue);
    }
}