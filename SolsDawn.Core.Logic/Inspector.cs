using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RangeAttribute(float min, float max) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class StepAttribute(float speed) : Attribute
{
    public float Speed { get; } = speed;
}

public static partial class AutoInspector
{
    [GeneratedRegex("(\\B[A-Z])")]
    private static partial Regex PascalCaseRegex();

    private class FieldMeta
    {
        public FieldInfo Field { get; set; } = null!;
        public InspectAttribute? InspectAttr { get; set; }
        public CategoryAttribute? CategoryAttr { get; set; }
        public float Speed { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsCategorySlot { get; set; }
    }

    private static readonly Dictionary<Type, List<FieldMeta>> _cache = new();
    private static readonly Dictionary<string, string> _presetNameDrafts = new();
    private static readonly Dictionary<string, string> _renameDrafts = new();

    private static string _librarySearchQuery = string.Empty;
    private static string _libraryTagFilter = string.Empty;

    public static Action? OnAnyValueChanged { get; set; }

    public static string FormatLabel(string rawName) => PascalCaseRegex().Replace(rawName, " $1");

    public static bool Draw(object target)
    {
        if (target == null)
            return false;

        CategorySystem.BindSlots(target);

        var type = target.GetType();
        if (!_cache.TryGetValue(type, out var metaList))
        {
            metaList = BuildMetaList(type);
            _cache[type] = metaList;
        }

        bool anyChanged = false;
        ImGui.PushID(target.GetHashCode());

        foreach (var meta in metaList)
        {
            var field = meta.Field;
            var label = meta.Label;

            if (meta.IsCategorySlot)
            {
                if (field.GetValue(target) is ICategorySlot slot)
                {
                    slot.Configure(meta.CategoryAttr, field.Name);
                    if (DrawCategorySlot(slot, label))
                        anyChanged = true;
                }
                continue;
            }

            if (field.FieldType == typeof(float))
            {
                var value = (float)field.GetValue(target)!;
                bool changed = meta.Min != float.MinValue && meta.Max != float.MaxValue
                    ? ImGui.SliderFloat(label, ref value, meta.Min, meta.Max)
                    : ImGui.DragFloat(label, ref value, meta.Speed, meta.Min, meta.Max);

                if (changed)
                {
                    field.SetValue(target, value);
                    anyChanged = true;
                }
            }
            else if (field.FieldType == typeof(int))
            {
                var value = (int)field.GetValue(target)!;
                if (ImGui.DragInt(label, ref value, Math.Max(1f, meta.Speed), (int)meta.Min, (int)meta.Max))
                {
                    field.SetValue(target, value);
                    anyChanged = true;
                }
            }
            else if (field.FieldType == typeof(bool))
            {
                var value = (bool)field.GetValue(target)!;
                if (ImGui.Checkbox(label, ref value))
                {
                    field.SetValue(target, value);
                    anyChanged = true;
                }
            }
            else if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                var nestedObj = field.GetValue(target);
                if (nestedObj != null)
                {
                    ImGui.PushID(field.Name);
                    if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        if (Draw(nestedObj))
                            anyChanged = true;
                        ImGui.TreePop();
                    }
                    ImGui.PopID();
                }
            }
        }

        ImGui.PopID();

        if (anyChanged)
            OnAnyValueChanged?.Invoke();

        return anyChanged;
    }

    private static List<FieldMeta> BuildMetaList(Type type)
    {
        var metaList = new List<FieldMeta>();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var inspectAttr = field.GetCustomAttribute<InspectAttribute>();
            var categoryAttr = field.GetCustomAttribute<CategoryAttribute>();
            bool isSlot = typeof(ICategorySlot).IsAssignableFrom(field.FieldType);

            if (inspectAttr == null && categoryAttr == null && !isSlot)
                continue;

            var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
            var stepAttr = field.GetCustomAttribute<StepAttribute>();

            float min = rangeAttr?.Min ?? inspectAttr?.Min ?? float.MinValue;
            float max = rangeAttr?.Max ?? inspectAttr?.Max ?? float.MaxValue;
            float speed = stepAttr?.Speed ?? inspectAttr?.Speed ?? 0.1f;
            string label = inspectAttr?.CustomName ?? FormatLabel(field.Name);

            metaList.Add(new FieldMeta
            {
                Field = field,
                InspectAttr = inspectAttr,
                CategoryAttr = categoryAttr,
                Min = min,
                Max = max,
                Speed = speed,
                Label = label,
                IsCategorySlot = isSlot
            });
        }

        return metaList;
    }

    private static bool DrawCategorySlot(ICategorySlot slot, string categoryDisplayName)
    {
        bool changed = false;
        var state = slot.State;
        var activeInstance = slot.GetActiveInstanceObject();
        var activeType = activeInstance.GetType();
        var slotId = $"{slot.GetHashCode()}_{slot.FieldName}";

        ImGui.PushID(slotId);

        // Tint red if Error_Missing
        int pushedColors = 0;
        if (state == SlotState.Error_Missing)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.65f, 0.12f, 0.12f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.75f, 0.18f, 0.18f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.85f, 0.22f, 0.22f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
            pushedColors = 4;
        }

        // 1. Slot Header: Category Name + Active Model Type
        string headerTitle = $"{categoryDisplayName} [{activeType.Name}]###SlotHeader_{slot.FieldName}";
        bool isOpen = ImGui.CollapsingHeader(headerTitle, ImGuiTreeNodeFlags.DefaultOpen);

        if (isOpen)
        {
            ImGui.Indent(10f);

            // 2. State Indicator & Missing Asset Warning
            DrawStateIndicator(slot, state);

            // 3. Model Selector (Dropdown) via Resolution Handshake
            var allowedModels = CategoryRegistry.GetAllowedModels(slot.CategoryAttribute, slot.ModelInterfaceType);
            if (ImGui.BeginCombo("Model", activeType.Name))
            {
                foreach (var descriptor in allowedModels)
                {
                    bool isSelected = descriptor.ModelType == activeType && state == SlotState.Inline;
                    if (ImGui.Selectable(descriptor.Name, isSelected))
                    {
                        slot.SelectModel(descriptor.ModelType);
                        activeInstance = slot.GetActiveInstanceObject();
                        activeType = activeInstance.GetType();
                        state = slot.State;
                        changed = true;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // 4. Link / Unlink Authoring Controls
            DrawAuthoringControls(slot, slotId, activeType.Name, ref changed);

            // 5. Value Inspector (recursive AutoInspector on GetActiveInstance())
            if (state == SlotState.Linked)
            {
                var presetEntry = Library.GetPresetEntry(slot.PresetGuid);
                string presetTitle = presetEntry?.Name ?? slot.PresetGuid ?? "Preset";
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f),
                    $"Editing Global Preset '{presetTitle}' (changes affect all users of this preset)");
            }

            bool instanceEdited = Draw(activeInstance);
            if (instanceEdited)
            {
                if (state == SlotState.Linked && !string.IsNullOrWhiteSpace(slot.PresetGuid))
                {
                    Library.SavePreset(slot.PresetGuid);
                }
                changed = true;
            }

            ImGui.Unindent(10f);
            ImGui.Spacing();
        }

        if (pushedColors > 0)
            ImGui.PopStyleColor(pushedColors);

        ImGui.PopID();
        return changed;
    }

    private static void DrawStateIndicator(ICategorySlot slot, SlotState state)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled("State:");
        ImGui.SameLine();

        switch (state)
        {
            case SlotState.Inline:
                ImGui.TextColored(new Vector4(0.35f, 0.90f, 0.45f, 1.0f), "[Inline (Unique)]");
                break;

            case SlotState.Linked:
                var entry = Library.GetPresetEntry(slot.PresetGuid);
                ImGui.TextColored(new Vector4(0.35f, 0.75f, 1.0f, 1.0f),
                    $"[Linked Preset: {entry?.Name ?? slot.PresetGuid}]");
                break;

            case SlotState.Error_Missing:
                ImGui.TextColored(new Vector4(1.0f, 0.25f, 0.25f, 1.0f), "[ERROR: MISSING]");
                ImGui.TextColored(new Vector4(1.0f, 0.30f, 0.30f, 1.0f),
                    $"Missing Asset: [{slot.PresetGuid}]. Using default fallback.");
                break;
        }
    }

    private static void DrawAuthoringControls(ICategorySlot slot, string slotId, string activeModelName, ref bool changed)
    {
        if (!_presetNameDrafts.TryGetValue(slotId, out var draftName))
        {
            draftName = $"{slot.FieldName}_{activeModelName}";
            _presetNameDrafts[slotId] = draftName;
        }

        if (ImGui.Button("Save as Library Preset"))
        {
            _presetNameDrafts[slotId] = $"{slot.FieldName}_{activeModelName}";
            ImGui.OpenPopup("SavePresetPopup");
        }

        if (ImGui.BeginPopup("SavePresetPopup"))
        {
            ImGui.Text("Preset Name:");
            ImGui.InputText("##PresetNameInput", ref draftName, 64);
            _presetNameDrafts[slotId] = draftName;

            if (ImGui.Button("Confirm Save"))
            {
                slot.SaveAsPreset(draftName);
                changed = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Load Library Preset"))
        {
            ImGui.OpenPopup("LoadPresetPopup");
        }

        if (ImGui.BeginPopup("LoadPresetPopup"))
        {
            var compatiblePresets = Library.GetAllowedPresets(slot.CategoryAttribute, slot.ModelInterfaceType);
            if (compatiblePresets.Count == 0)
            {
                ImGui.TextDisabled("No compatible presets in Library.");
            }
            else
            {
                foreach (var preset in compatiblePresets)
                {
                    bool isCurrent = slot.IsPreset && string.Equals(slot.PresetGuid, preset.Guid, StringComparison.OrdinalIgnoreCase);
                    string itemLabel = $"{preset.Name} ({preset.Instance?.GetType().Name})##{preset.Guid}";
                    if (ImGui.Selectable(itemLabel, isCurrent))
                    {
                        slot.LinkPreset(preset.Guid);
                        changed = true;
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
            ImGui.EndPopup();
        }

        if (slot.IsPreset)
        {
            ImGui.SameLine();
            if (ImGui.Button("Make Unique"))
            {
                slot.MakeUnique();
                changed = true;
            }
        }
    }

    // 6. Library Browser Window
    public static void DrawLibraryBrowser(ref bool isOpen)
    {
        if (!isOpen)
            return;

        ImGui.SetNextWindowSize(new Vector2(520, 420), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Library Browser", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ImGui.InputText("Filter Name / Model", ref _librarySearchQuery, 64);
        ImGui.InputText("Filter Category Tags", ref _libraryTagFilter, 64);
        ImGui.Separator();

        var presets = Library.GetAllPresets().ToList();
        if (presets.Count == 0)
        {
            ImGui.TextDisabled("No saved presets in /Presets/ folder.");
            ImGui.End();
            return;
        }

        string? guidToDelete = null;

        foreach (var preset in presets)
        {
            var modelName = preset.Instance?.GetType().Name ?? preset.ModelType;

            if (!string.IsNullOrWhiteSpace(_librarySearchQuery) &&
                !preset.Name.Contains(_librarySearchQuery, StringComparison.OrdinalIgnoreCase) &&
                !modelName.Contains(_librarySearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(_libraryTagFilter) &&
                !preset.CategoryTags.Contains(_libraryTagFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ImGui.PushID(preset.Guid);

            bool nodeOpen = ImGui.TreeNodeEx($"{preset.Name} [{modelName}]###PresetNode_{preset.Guid}", ImGuiTreeNodeFlags.FramePadding);

            if (nodeOpen)
            {
                ImGui.TextDisabled($"GUID: {preset.Guid}");
                ImGui.TextDisabled($"Tags: {preset.CategoryTags}");

                if (!_renameDrafts.TryGetValue(preset.Guid, out var renameDraft))
                {
                    renameDraft = preset.Name;
                    _renameDrafts[preset.Guid] = renameDraft;
                }

                ImGui.SetNextItemWidth(200f);
                if (ImGui.InputText("##RenamePreset", ref renameDraft, 64))
                {
                    _renameDrafts[preset.Guid] = renameDraft;
                }
                ImGui.SameLine();
                if (ImGui.Button("Rename"))
                {
                    Library.RenamePreset(preset.Guid, renameDraft);
                }
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.75f, 0.15f, 0.15f, 1.0f));
                if (ImGui.Button("Delete"))
                {
                    guidToDelete = preset.Guid;
                }
                ImGui.PopStyleColor();

                if (preset.Instance != null)
                {
                    ImGui.Spacing();
                    if (Draw(preset.Instance))
                    {
                        Library.SavePreset(preset.Guid);
                    }
                }

                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        if (guidToDelete != null)
        {
            Library.DeletePreset(guidToDelete);
        }

        ImGui.End();
    }
}