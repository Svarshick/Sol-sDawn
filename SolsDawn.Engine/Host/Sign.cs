using System.Numerics;

namespace SolsDawn.Engine;

internal readonly struct Sign
{
    private readonly sbyte _value;

    private Sign(sbyte value) => _value = value;

    public static readonly Sign Negative = new(-1);
    public static readonly Sign Zero = new(0);
    public static readonly Sign Positive = new(1);

    public static implicit operator Sign(double x) => new((sbyte)(x > 0 ? 1 : x < 0 ? -1 : 0));

    public static implicit operator int(Sign s) => s._value;

    public override string ToString() => _value.ToString();
}

internal static class SignExtensions
{
    public static Sign Sign<T>(this T x) where T : INumber<T> => T.IsNaN(x) ? 0 : T.Sign(x);
}
