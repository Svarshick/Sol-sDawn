using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

public enum DebugCategory
{
    Default,
    Parry,
    Attack
}

public static class Debug
{
    public static bool ColliderEnabled = true;
    public static Color ColliderColor = Color.LawnGreen;
    public static float ColliderMinimalTime = 0.5f;

    public static bool ParryEnabled = true;
    public static Color ParryColor = Color.DodgerBlue;
    public static float ParryMinimalTime = 0.5f;

    public static bool AttackEnabled = true;
    public static Color AttackColor = Color.Red;
    public static float AttackMinimalTime = 0.5f;
}