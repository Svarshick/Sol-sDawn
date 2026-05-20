using System;

namespace SolsDawn.Core.Logic.Gameplay;

public sealed class Hp : Component<Hp>
{
    public int Max { get; private set; }
    public int Current { get; private set; }

    public bool IsDied => Current <= 0;
    public event Action<int> Damaged;
    public event Action<int> Healed;

    public Hp(GameObject go, int max) : base(go)
    {
        Max = max;
        Current = max;
    }

    public override void Dispose()
    {
    }

    public void Damage(int value)
    {
        if (value < 0)
            throw  new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be negative.");

        Current = Math.Max(0, Current - value);
        Damaged?.Invoke(value);
    }

    public void Heal(int value)
    {
        if (value < 0)
            throw  new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be negative.");

        Current = Math.Min(Max, Current + value);
        Healed?.Invoke(value);
    }
}