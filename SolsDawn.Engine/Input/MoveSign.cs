using System;
using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal static class InputProjection
{
    public static Sign Get(Vector2 move, Vector2 axis)
    {
        var dot = Vector2.Dot(move, axis);
        return MathF.Abs(dot) < 0.001f ? Sign.Zero : dot;
    }
}

internal class InputHolding
{
    private Input.MoveKeys _tracked;
    private Sign _sign;

    public Sign Get(Input.MoveKeys held, Vector2 axis)
    {
        if (_tracked != Input.MoveKeys.None && (held & _tracked) == _tracked)
            return _sign;

        _sign = InputProjection.Get(Input.Direction(held), axis);
        _tracked = _sign == 0 ? Input.MoveKeys.None : held;
        return _sign;
    }
}
