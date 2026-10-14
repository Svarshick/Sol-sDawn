using System;
using System.Collections.Generic;
using System.Linq;
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

public static partial class AutoInspector
{
    [GeneratedRegex("(\\B[A-Z])")]
    private static partial Regex PascalCaseRegex();
    
    private class FieldMeta
    {
        public FieldInfo Field { get; set; }
        public InspectAttribute? Attr { get; set; } // Nullable now, since CategorySlots might not have Inspect
        public CategoryAttribute? CatAttr { get; set; }
        public string Label { get; set; }
    }

    private static readonly Dictionary<Type, List<FieldMeta>> _cache = new();

    public static void Draw(object target)
    {
        if (target == null) return;
        var type = target.GetType();

        if (!_cache.TryGetValue(type, out var metaList))
        {
            metaList = new List<FieldMeta>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var inspectAttr = field.GetCustomAttribute<InspectAttribute>();
                var catAttr = field.GetCustomAttribute<CategoryAttribute>();
                
                // Allow drawing if it has [Inspect] OR if it's a CategorySlot
                if (inspectAttr != null || catAttr != null)
                {
                    var label = inspectAttr?.CustomName ?? PascalCaseRegex().Replace(field.Name, " $1");
                    metaList.Add(new FieldMeta { Field = field, Attr = inspectAttr, CatAttr = catAttr, Label = label });
                }
            }
            _cache[type] = metaList;
        }

        ImGui.PushID(type.Name);

        foreach (var meta in metaList)
        {
            var field = meta.Field;
            var attr = meta.Attr;
            var label = meta.Label;

            // Handle Category Slots natively in the inspector
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(CategorySlot<>))
            {
                var slot = field.GetValue(target);
                if (slot != null && meta.CatAttr != null)
                {
                    DrawCategorySlot(label, slot, meta.CatAttr);
                }
                continue;
            }

            if (attr == null) continue; // standard inspect attributes below

            if (field.FieldType == typeof(float))
            {
                var value = (float)field.GetValue(target)!;
                bool changed = attr.Min != float.MinValue && attr.Max != float.MaxValue
                    ? ImGui.SliderFloat(label, ref value, attr.Min, attr.Max)
                    : ImGui.DragFloat(label, ref value, attr.Speed, attr.Min, attr.Max);
                if (changed) field.SetValue(target, value);
            }
            else if (field.FieldType == typeof(int))
            {
                var value = (int)field.GetValue(target)!;
                if (ImGui.DragInt(label, ref value, (int)Math.Max(1, attr.Speed)))
                    field.SetValue(target, value);
            }
            else if (field.FieldType == typeof(bool))
            {
                var value = (bool)field.GetValue(target)!;
                if (ImGui.Checkbox(label, ref value))
                    field.SetValue(target, value);
            }
            else if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                var nestedValue = field.GetValue(target);
                if (nestedValue != null)
                {
                    if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Draw(nestedValue);
                        ImGui.TreePop();
                    }
                }
            }
        }
        
        ImGui.PopID();
    }

    private static void DrawCategorySlot(string label, object slot, CategoryAttribute catAttr)
    {
        var slotType = slot.GetType();
        var state = (SlotState)slotType.GetMethod(nameof(CategorySlot<object>.GetState))!.Invoke(slot, null)!;
        var activeInstance = slotType.GetMethod(nameof(CategorySlot<object>.GetActiveInstance))!.Invoke(slot, null)!;
        
        var isPresetProp = slotType.GetProperty(nameof(CategorySlot<object>.IsPreset))!;
        var inlineDataProp = slotType.GetProperty(nameof(CategorySlot<object>.InlineData))!;
        
        ImGui.PushID(label);
        ImGui.Spacing();
        
        // Header & State Indicator
        ImGui.TextColored(state == SlotState.Error_Missing ? new System.Numerics.Vector4(1, 0, 0, 1) : new System.Numerics.Vector4(0, 1, 0, 1), 
            $"[ {state} ]");
        ImGui.SameLine();
        ImGui.Text(label + ": " + activeInstance.GetType().Name);

        // Allowed Model Selector (if not linked)
        if (state != SlotState.Linked)
        {
            var allowedModels = ConfigurationUtility.GetAllowedModelsForCategory(catAttr).ToList();
            if (ImGui.BeginCombo("Model", activeInstance.GetType().Name))
            {
                foreach (var model in allowedModels)
                {
                    if (ImGui.Selectable(model.Name, activeInstance.GetType() == model))
                    {
                        var newInstance = Activator.CreateInstance(model);
                        inlineDataProp.SetValue(slot, newInstance);
                    }
                }
                ImGui.EndCombo();
            }
        }

        // Action Buttons
        if (state == SlotState.Linked)
        {
            if (ImGui.Button("Make Unique"))
            {
                slotType.GetMethod(nameof(CategorySlot<object>.MakeUnique))!.Invoke(slot, null);
            }
        }
        else
        {
            if (ImGui.Button("Save as Preset"))
            {
                string newGuid = Guid.NewGuid().ToString();
                Library.SavePreset(newGuid, activeInstance);
                slotType.GetMethod(nameof(CategorySlot<object>.LinkPreset))!.Invoke(slot, new object[] { newGuid });
            }
        }

        // Draw Nested Fields inline!
        ImGui.Indent();
        Draw(activeInstance); 
        ImGui.Unindent();

        ImGui.Separator();
        ImGui.PopID();
    }
}