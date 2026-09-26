using System;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace SolsDawn.Core.Logic;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InspectAttribute : Attribute
{
    public float Speed { get; }
    public float Min { get; }
    public float Max { get; }
    public string? CustomName { get; }

    public InspectAttribute(float speed = 0.1f, float min = float.MinValue, float max = float.MaxValue, string? customName = null)
    {
        Speed = speed;
        Min = min;
        Max = max;
        CustomName = customName;
    }
}

public static class AutoInspector
{
    public static void Draw(object target)
    {
        if (target == null) return;

        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<InspectAttribute>();
            if (attr == null) 
                continue;

            string label = attr.CustomName ?? Regex.Replace(field.Name, "(\\B[A-Z])", " $1");

            if (field.FieldType == typeof(float))
            {
                float value = (float)field.GetValue(target)!;
                
                bool changed = attr.Min != float.MinValue && attr.Max != float.MaxValue
                    ? ImGui.SliderFloat(label, ref value, attr.Min, attr.Max)
                    : ImGui.DragFloat(label, ref value, attr.Speed, attr.Min, attr.Max);

                if (changed)
                    field.SetValue(target, value);
            }
            else if (field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(target)!;
                if (ImGui.DragInt(label, ref value, (int)Math.Max(1, attr.Speed)))
                    field.SetValue(target, value);
            }
            else if (field.FieldType == typeof(bool))
            {
                bool value = (bool)field.GetValue(target)!;
                if (ImGui.Checkbox(label, ref value))
                    field.SetValue(target, value);
            }
        }
    }
}