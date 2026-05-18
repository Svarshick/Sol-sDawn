using System.Reflection;

namespace SolsDawn.Core.Logic.Configs;

public static class ConfigReader
{
    public static T Read<T>(T sourceConfig, ScreenLayout layout) where T : class, new()
    {
        var config = new T();
        var type = typeof(T);

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(sourceConfig);

            if (field.GetCustomAttribute<UnitsAttribute>() != null && value is float sourceFloat)
            {
                value = layout.ToPixels(sourceFloat);
            }

            field.SetValue(config, value);
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            var value = prop.GetValue(sourceConfig);

            if (prop.GetCustomAttribute<UnitsAttribute>() != null && value is float sourceFloat)
            {
                value = layout.ToPixels(sourceFloat);
            }

            prop.SetValue(config, value);
        }

        return config;
    }
}