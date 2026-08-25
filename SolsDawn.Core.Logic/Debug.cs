using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

public static class Debug
{
    public static class Collider
    {
        public enum Category
        {
            Default,
            Parry,
            Attack
        }

        public static bool DefaultEnabled = true;
        public static Color DefaultColor = Color.LawnGreen;
        public static float DefaultMinimalTime = 0.5f;
        public static float DefaultThickness = 0.05f;

        public static bool ParryEnabled = true;
        public static Color ParryColor = Color.DodgerBlue;
        public static float ParryMinimalTime = 0.5f;
        public static float ParryThickness = 0.05f;

        public static bool AttackEnabled = true;
        public static Color AttackColor = Color.Red;
        public static float AttackMinimalTime = 0.5f;
        public static float AttackThickness = 0.05f;
    }
}