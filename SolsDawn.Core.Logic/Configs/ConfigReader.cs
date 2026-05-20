using System.Reflection;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Configs;

public static class ConfigReader
{
    public static T Read<T>(T sourceConfig, ScreenLayout layout) where T : class, new()
    {
        var config = new T();

        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(sourceConfig);

            if (field.GetCustomAttribute<UnitsAttribute>() != null && value is float sourceFloat)
            {
                value = layout.ToPixels(sourceFloat);
            }
            else if (field.GetCustomAttribute<EulerAttribute>() != null && value is float sourceEuler)
            {
                value = MathHelper.ToRadians(sourceEuler);
            }

            field.SetValue(config, value);
        }

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            var value = prop.GetValue(sourceConfig);

            if (prop.GetCustomAttribute<UnitsAttribute>() != null && value is float sourceFloat)
            {
                value = layout.ToPixels(sourceFloat);
            }
            else if (prop.GetCustomAttribute<EulerAttribute>() != null && value is float sourceEuler)
            {
                value = layout.ToPixels(sourceEuler);
            }

            prop.SetValue(config, value);
        }

        return config;
    }
}