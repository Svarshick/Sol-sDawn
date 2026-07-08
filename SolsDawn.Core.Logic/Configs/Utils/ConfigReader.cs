using System.Reflection;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Configs.Utils;

public static class ConfigReader
{
    public static T Read<T>(T sourceConfig) where T : class, new()
    {
        var config = new T();

        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(sourceConfig);

            if (field.GetCustomAttribute<EulerAttribute>() != null && value is float sourceEuler)
            {
                value = MathHelper.ToRadians(sourceEuler);
            }

            field.SetValue(config, value);
        }

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            var value = prop.GetValue(sourceConfig);

            if (prop.GetCustomAttribute<EulerAttribute>() != null && value is float sourceEuler)
            {
                value = MathHelper.ToRadians(sourceEuler);
            }

            prop.SetValue(config, value);
        }

        return config;
    }
}