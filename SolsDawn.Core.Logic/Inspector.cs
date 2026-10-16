using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using NumVector2 = System.Numerics.Vector2;
using NumVector4 = System.Numerics.Vector4;

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

public static class AutoInspector
{
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
        public bool IsReadOnly { get; set; }
    }

    private sealed record CachedSearchState(
        string Query,
        int LibraryVersion,
        IReadOnlyList<ModelSearchResult> Results);

    // Section 4: ImGui Palette Constants
    private static readonly NumVector4 ColorCurrentText        = new(1.00f, 1.00f, 1.00f, 1.00f); // White (Default)
    private static readonly NumVector4 ColorCurrentHoverBg     = new(0.16f, 0.29f, 0.48f, 0.85f); // Dark Blue (ImGui Default Active)

    private static readonly NumVector4 ColorReferenceText      = new(0.86f, 0.88f, 0.90f, 1.00f); // Light Gray (Default)
    private static readonly NumVector4 ColorReferenceHoverBg   = new(0.26f, 0.36f, 0.48f, 0.55f); // Muted Gray-Blue (ImGui Default Hover)

    private static readonly NumVector4 ColorCopyText           = new(0.45f, 0.82f, 0.78f, 1.00f); // Muted Teal (Soft Teal)
    private static readonly NumVector4 ColorCopyHoverBg        = new(0.12f, 0.65f, 0.62f, 0.45f); // Bright Teal (with transparency)

    private static readonly NumVector4 ColorDefaultTemplateText = new(0.50f, 0.54f, 0.58f, 1.00f); // Dark Gray / Dimmed

    // Section 5.2: [ X ] Remove from History Button Colors
    private static readonly NumVector4 ColorRemoveBtnBg        = new(0.35f, 0.07f, 0.09f, 0.90f); // Dark Maroon
    private static readonly NumVector4 ColorRemoveBtnText      = new(0.82f, 0.42f, 0.42f, 1.00f); // Maroon-tinted text
    private static readonly NumVector4 ColorRemoveBtnHoverBg   = new(0.90f, 0.16f, 0.16f, 1.00f); // Bright Red
    private static readonly NumVector4 ColorRemoveBtnHoverText = new(1.00f, 0.95f, 0.95f, 1.00f); // Bright highlight text

    private static readonly Dictionary<Type, List<FieldMeta>> _cache = new();

    // UI state per CategorySlot
    private static readonly Dictionary<string, bool> _slotExpanded = new();
    private static readonly Dictionary<string, bool> _slotExpandedBeforeClick = new();
    private static readonly Dictionary<string, string> _slotSearchQueries = new();
    private static readonly Dictionary<string, CachedSearchState> _slotSearchCache = new();
    private static readonly Dictionary<string, string> _modelNameDrafts = new();
    private static string? _pendingSearchOpenSlotId;
    private static int _drawDepth;

    // Library Browser state
    private static string _librarySearchQuery = string.Empty;
    private static string _libraryTagFilter = string.Empty;

    public static Action? OnAnyValueChanged { get; set; }

    public static string FormatLabel(string rawName) => rawName;

    public static bool Draw(object target)
    {
        if (target == null)
            return false;

        CategorySystem.BindSlots(target);

        var type = target.GetType();
        if (_drawDepth == 0 && type.GetCustomAttribute<ModelAttribute>(inherit: false) == null)
        {
            Library.RegisterRootOwner(target);
        }

        if (!_cache.TryGetValue(type, out var metaList))
        {
            metaList = BuildMetaList(type);
            _cache[type] = metaList;
        }

        bool anyChanged = false;
        _drawDepth++;
        ImGui.PushID(target.GetHashCode());

        try
        {
            foreach (var meta in metaList)
            {
                var field = meta.Field;
                var label = meta.Label;

                // 2.3. Category (CategorySlot)
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

                // 2.1. Regular Fields (float / int / bool / Vector2 / Color / string / Enum)
                if (IsRegularFieldType(field.FieldType))
                {
                    if (DrawRegularField(target, meta))
                        anyChanged = true;
                    continue;
                }

                // 2.2. Nested Field (container class with its own fields, e.g., MotionSolver)
                if (field.FieldType.IsClass && field.FieldType != typeof(string))
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
        }
        finally
        {
            ImGui.PopID();
            _drawDepth--;
        }

        if (anyChanged && _drawDepth == 0)
            OnAnyValueChanged?.Invoke();

        return anyChanged;
    }

    private static bool IsRegularFieldType(Type t)
    {
        return t == typeof(float) ||
               t == typeof(int) ||
               t == typeof(bool) ||
               t == typeof(string) ||
               t == typeof(Vector2) ||
               t == typeof(NumVector2) ||
               t == typeof(Color) ||
               t.IsEnum;
    }

    private static bool DrawRegularField(object target, FieldMeta meta)
    {
        var field = meta.Field;
        var label = meta.Label;
        bool isReadOnly = meta.IsReadOnly;
        bool changed = false;

        ImGui.PushID(field.Name);

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        if (isReadOnly)
            ImGui.BeginDisabled(true);

        if (field.FieldType == typeof(float))
        {
            var value = (float)field.GetValue(target)!;
            bool edited = meta.Min != float.MinValue && meta.Max != float.MaxValue
                ? ImGui.SliderFloat("##val", ref value, meta.Min, meta.Max)
                : ImGui.DragFloat("##val", ref value, meta.Speed, meta.Min, meta.Max);

            if (edited && !isReadOnly)
            {
                field.SetValue(target, value);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(int))
        {
            var value = (int)field.GetValue(target)!;
            bool edited = meta.Min != float.MinValue && meta.Max != float.MaxValue
                ? ImGui.SliderInt("##val", ref value, (int)meta.Min, (int)meta.Max)
                : ImGui.DragInt("##val", ref value, Math.Max(1f, meta.Speed), (int)meta.Min, (int)meta.Max);

            if (edited && !isReadOnly)
            {
                field.SetValue(target, value);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(bool))
        {
            var value = (bool)field.GetValue(target)!;
            if (ImGui.Checkbox("##val", ref value) && !isReadOnly)
            {
                field.SetValue(target, value);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(Vector2))
        {
            var xnaVec = (Vector2)field.GetValue(target)!;
            var numVec = new NumVector2(xnaVec.X, xnaVec.Y);
            if (ImGui.DragFloat2("##val", ref numVec, meta.Speed, meta.Min, meta.Max) && !isReadOnly)
            {
                field.SetValue(target, new Vector2(numVec.X, numVec.Y));
                changed = true;
            }
        }
        else if (field.FieldType == typeof(NumVector2))
        {
            var numVec = (NumVector2)field.GetValue(target)!;
            if (ImGui.DragFloat2("##val", ref numVec, meta.Speed, meta.Min, meta.Max) && !isReadOnly)
            {
                field.SetValue(target, numVec);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(Color))
        {
            var c = (Color)field.GetValue(target)!;
            var vec4 = new NumVector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            if (ImGui.ColorEdit4("##val", ref vec4, ImGuiColorEditFlags.NoInputs) && !isReadOnly)
            {
                field.SetValue(target, new Color(vec4.X, vec4.Y, vec4.Z, vec4.W));
                changed = true;
            }
        }
        else if (field.FieldType == typeof(string))
        {
            var str = (string?)field.GetValue(target) ?? string.Empty;
            if (ImGui.InputText("##val", ref str, 128) && !isReadOnly)
            {
                field.SetValue(target, str);
                changed = true;
            }
        }
        else if (field.FieldType.IsEnum)
        {
            var currentVal = field.GetValue(target)!;
            var names = Enum.GetNames(field.FieldType);
            int idx = Array.IndexOf(names, currentVal.ToString());
            if (idx < 0) idx = 0;
            if (ImGui.Combo("##val", ref idx, names, names.Length) && !isReadOnly)
            {
                var parsed = Enum.Parse(field.FieldType, names[idx]);
                field.SetValue(target, parsed);
                changed = true;
            }
        }

        if (isReadOnly)
            ImGui.EndDisabled();

        ImGui.PopID();
        return changed;
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
                IsCategorySlot = isSlot,
                IsReadOnly = field.IsInitOnly
            });
        }

        return metaList;
    }

    private static bool DrawCategorySlot(ICategorySlot slot, string categoryName)
    {
        bool changed = false;
        var slotId = $"{slot.GetHashCode()}_{slot.FieldName}";
        ImGui.PushID(slotId);

        if (!_slotExpanded.TryGetValue(slotId, out bool isExpanded))
        {
            isExpanded = false;
            _slotExpanded[slotId] = isExpanded;
        }

        string activeModelName = slot.GetActiveModelName();
        string arrow = isExpanded ? "v" : ">";
        string headerLabel = $"{arrow} {categoryName} [{activeModelName}]###CatHeader_{slot.FieldName}";

        int pushedColors = 0;
        if (slot.IsMissingModel)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, new NumVector4(0.65f, 0.12f, 0.12f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new NumVector4(0.75f, 0.18f, 0.18f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new NumVector4(0.85f, 0.22f, 0.22f, 1.00f));
            pushedColors = 3;
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Header, new NumVector4(0.22f, 0.27f, 0.36f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new NumVector4(0.28f, 0.35f, 0.48f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new NumVector4(0.32f, 0.42f, 0.58f, 1.00f));
            pushedColors = 3;
        }

        ImGui.Selectable(headerLabel, true, ImGuiSelectableFlags.AllowDoubleClick);
        var headerMin = ImGui.GetItemRectMin();
        var headerMax = ImGui.GetItemRectMax();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Click: Expand/Collapse Model Editor\nDouble-Click: Open Model Search");

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if (_slotExpandedBeforeClick.TryGetValue(slotId, out bool prevExpanded))
                {
                    isExpanded = prevExpanded;
                    _slotExpanded[slotId] = isExpanded;
                }

                _pendingSearchOpenSlotId = slotId;
                if (!_slotSearchQueries.ContainsKey(slotId))
                    _slotSearchQueries[slotId] = string.Empty;

                _slotSearchCache.Remove(slotId);
                ImGui.OpenPopup("CategorySearchPopup");
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _slotExpandedBeforeClick[slotId] = isExpanded;
                isExpanded = !isExpanded;
                _slotExpanded[slotId] = isExpanded;
            }
        }

        if (pushedColors > 0)
            ImGui.PopStyleColor(pushedColors);

        // Model Selector Search Popup
        if (DrawCategorySearchPopup(slot, slotId, headerMin, headerMax))
        {
            changed = true;
            _slotExpanded[slotId] = true;
            isExpanded = true;
        }

        // Expanded Model Editor
        if (isExpanded)
        {
            ImGui.Indent(12f);

            if (slot.IsMissingModel)
            {
                ImGui.TextColored(new NumVector4(1.0f, 0.3f, 0.3f, 1.0f),
                    $"Missing Model [{slot.ModelGuid}]. Using default fallback.");
            }

            if (DrawModelEditor(slot))
            {
                changed = true;
            }

            ImGui.Unindent(12f);
            ImGui.Spacing();
        }

        ImGui.PopID();
        return changed;
    }

    private static IReadOnlyList<ModelSearchResult> GetOrRunSearch(ICategorySlot slot, string slotId, string query)
    {
        int currentVersion = Library.Version;
        if (_slotSearchCache.TryGetValue(slotId, out var cached) &&
            cached.LibraryVersion == currentVersion &&
            string.Equals(cached.Query, query, StringComparison.Ordinal))
        {
            return cached.Results;
        }

        var results = Library.SearchModels(slot.CategoryAttribute, slot.ModelInterfaceType, query);
        _slotSearchCache[slotId] = new CachedSearchState(query, Library.Version, results);
        return results;
    }

    private static bool DrawCategorySearchPopup(
        ICategorySlot slot,
        string slotId,
        NumVector2 headerMin,
        NumVector2 headerMax)
    {
        bool changed = false;
        float width = Math.Max(headerMax.X - headerMin.X, 420f);
        ImGui.SetNextWindowPos(new NumVector2(headerMin.X, headerMax.Y));
        ImGui.SetNextWindowSize(new NumVector2(width, 310f), ImGuiCond.Always);

        if (!ImGui.BeginPopup("CategorySearchPopup"))
            return false;

        if (!_slotSearchQueries.TryGetValue(slotId, out var query))
            query = string.Empty;

        if (_pendingSearchOpenSlotId == slotId)
        {
            ImGui.SetKeyboardFocusHere();
            _pendingSearchOpenSlotId = null;
        }

        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextWithHint("##ModelSearchInput", "Search (e.g. has:Model had:Model default: pattern)...", ref query, 128))
        {
            _slotSearchQueries[slotId] = query;
            _slotSearchCache.Remove(slotId);
        }

        bool shiftHeld = ImGui.GetIO().KeyShift;
        ImGui.TextDisabled(shiftHeld
            ? "[SHIFT HELD] Owned -> Reference | Orphaned -> Copy | Default -> Copy"
            : "Owned -> Copy | Orphaned -> Reference | Default -> Copy (Hold Shift to invert)");
        ImGui.Separator();

        // Section 6: IsOrphaned is pre-calculated in Library.SearchModels and cached in ModelSearchResult
        var results = GetOrRunSearch(slot, slotId, query);

        ImGui.BeginChild("##SearchResultsList", new NumVector2(0, 0), ImGuiChildFlags.None);

        if (results.Count == 0)
        {
            ImGui.TextDisabled("No matching models found.");
        }
        else
        {
            float removeBtnWidth = ImGui.CalcTextSize("[ X ]").X + ImGui.GetStyle().FramePadding.X * 2f;
            string? guidToRemoveFromHistory = null;

            for (int i = 0; i < results.Count; i++)
            {
                var res = results[i];
                ImGui.PushID($"{res.Guid}_{res.ParentGuid ?? "root"}_{i}");

                bool isCurrent = !res.IsDefault &&
                                 string.Equals(slot.ModelGuid, res.Guid, StringComparison.OrdinalIgnoreCase);

                // Section 3: Interaction Logic based exclusively on pre-calculated IsOrphaned & Shift
                // - Default templates: Always Copy
                // - Orphaned: LMB = Reference, Shift+LMB = Copy
                // - Owned: LMB = Copy, Shift+LMB = Reference
                bool willCopy = res.IsDefault || (res.IsOrphaned ? shiftHeld : !shiftHeld);

                // Section 4: Color Scheme & Visualization (swaps in real-time with Shift)
                NumVector4 textColor;
                NumVector4 hoverBgColor;

                if (isCurrent)
                {
                    textColor = ColorCurrentText;
                    hoverBgColor = ColorCurrentHoverBg;
                }
                else if (res.IsDefault)
                {
                    textColor = ColorDefaultTemplateText;
                    hoverBgColor = ColorCopyHoverBg;
                }
                else if (willCopy)
                {
                    textColor = ColorCopyText;
                    hoverBgColor = ColorCopyHoverBg;
                }
                else
                {
                    textColor = ColorReferenceText;
                    hoverBgColor = ColorReferenceHoverBg;
                }

                float rowStartX = ImGui.GetCursorPosX();
                float rowAvailWidth = ImGui.GetContentRegionAvail().X;
                float selectableWidth = !res.IsDefault
                    ? Math.Max(60f, rowAvailWidth - removeBtnWidth - 6f)
                    : rowAvailWidth;

                ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverBgColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, hoverBgColor);
                ImGui.PushStyleColor(ImGuiCol.Header, ColorCurrentHoverBg);

                // Section 5.1: Left Side (Selectable area)
                bool rowClicked = ImGui.Selectable(
                    $"{res.DisplayLabel}###RowSelect",
                    isCurrent,
                    ImGuiSelectableFlags.AllowOverlap | ImGuiSelectableFlags.NoAutoClosePopups,
                    new NumVector2(selectableWidth, 0f));

                bool selectableHovered = ImGui.IsItemHovered();

                ImGui.PopStyleColor(4);

                if (selectableHovered)
                {
                    DrawSearchResultTooltip(res, willCopy);
                }

                // Section 5.2: Right Side (Remove from History [ X ] button)
                bool removeClicked = false;
                if (!res.IsDefault)
                {
                    ImGui.SameLine();
                    float buttonX = rowStartX + rowAvailWidth - removeBtnWidth;
                    if (buttonX > ImGui.GetCursorPosX())
                    {
                        ImGui.SetCursorPosX(buttonX);
                    }

                    ImGui.PushStyleColor(ImGuiCol.Button, ColorRemoveBtnBg);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorRemoveBtnHoverBg);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorRemoveBtnHoverBg);
                    ImGui.PushStyleColor(ImGuiCol.Text, ColorRemoveBtnText);

                    if (ImGui.SmallButton(" X "))
                    {
                        removeClicked = true;
                        guidToRemoveFromHistory = res.Guid;
                    }

                    bool btnHovered = ImGui.IsItemHovered();
                    ImGui.PopStyleColor(4);

                    if (btnHovered)
                    {
                        ImGui.SetTooltip("Remove from this slot's history");
                    }
                }

                // Guard so clicking [ X ] never triggers the Selectable
                if (rowClicked && !removeClicked)
                {
                    if (willCopy)
                    {
                        slot.CopyModel(res);
                    }
                    else
                    {
                        slot.ReferenceModel(res.Guid);
                    }

                    _slotSearchCache.Remove(slotId);
                    changed = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.PopID();
            }

            if (guidToRemoveFromHistory != null)
            {
                slot.RemoveFromHistory(guidToRemoveFromHistory);
                _slotSearchCache.Remove(slotId);
                changed = true;
            }
        }

        ImGui.EndChild();
        ImGui.EndPopup();
        return changed;
    }

    private static void DrawSearchResultTooltip(ModelSearchResult res, bool willCopy)
    {
        string pendingAction = willCopy ? "Copy (Create Unique Instance)" : "Reference (Link Existing GUID)";
        string controlsHint;
        string stateLabel;

        if (res.IsDefault)
        {
            stateLabel = "Default Template (New)";
            controlsHint = "LMB: Copy | Shift+LMB: Copy (Always Copy)";
        }
        else if (res.IsOrphaned)
        {
            stateLabel = "Orphaned";
            controlsHint = "LMB: Reference | Shift+LMB: Copy";
        }
        else
        {
            stateLabel = "Owned";
            controlsHint = "LMB: Copy | Shift+LMB: Reference";
        }

        ImGui.SetTooltip(
            $"Pending Action: {pendingAction}\n" +
            $"{controlsHint}\n" +
            $"State: {stateLabel} | Type: {res.ModelType.Name}");
    }

    private static bool DrawModelEditor(ICategorySlot slot)
    {
        bool changed = false;
        var activeInstance = slot.GetActiveInstanceObject();
        var modelEntry = slot.GetActiveModelEntry();
        var activeGuid = slot.ModelGuid ?? modelEntry?.Guid ?? "default";

        string currentName = modelEntry?.Name ?? activeInstance.GetType().Name;
        if (!_modelNameDrafts.TryGetValue(activeGuid, out var nameDraft) ||
            !ImGui.IsAnyItemActive())
        {
            nameDraft = currentName;
            _modelNameDrafts[activeGuid] = nameDraft;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("ModelName");
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        bool isDefaultEntry = modelEntry?.IsDefault ?? false;
        if (isDefaultEntry)
            ImGui.BeginDisabled(true);

        if (ImGui.InputText("##ModelNameEdit", ref nameDraft, 64))
        {
            nameDraft = nameDraft.Replace(' ', '_');
            _modelNameDrafts[activeGuid] = nameDraft;

            if (!isDefaultEntry && !string.IsNullOrWhiteSpace(slot.ModelGuid) && !string.IsNullOrWhiteSpace(nameDraft))
            {
                Library.RenameModel(slot.ModelGuid, nameDraft);
                changed = true;
            }
        }

        if (isDefaultEntry)
            ImGui.EndDisabled();

        bool contentsEdited = Draw(activeInstance);
        if (contentsEdited)
        {
            if (!isDefaultEntry && !string.IsNullOrWhiteSpace(slot.ModelGuid))
            {
                Library.SaveModel(slot.ModelGuid);
            }
            changed = true;
        }

        return changed;
    }

    public static void DrawLibraryBrowser(ref bool isOpen)
    {
        if (!isOpen)
            return;

        ImGui.SetNextWindowSize(new NumVector2(520, 420), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Library Browser", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ImGui.InputText("FilterName", ref _librarySearchQuery, 64);
        ImGui.InputText("FilterTags", ref _libraryTagFilter, 64);
        ImGui.Separator();

        var models = Library.GetAllModels()
            .OrderByDescending(m => m.LastUpdatedTicks)
            .ToList();

        if (models.Count == 0)
        {
            ImGui.TextDisabled("No saved models in /Presets/ folder.");
            ImGui.End();
            return;
        }

        string? guidToDelete = null;

        foreach (var model in models)
        {
            var typeName = model.Instance?.GetType().Name ?? model.ModelType;

            if (!string.IsNullOrWhiteSpace(_librarySearchQuery) &&
                !model.Name.Contains(_librarySearchQuery, StringComparison.OrdinalIgnoreCase) &&
                !typeName.Contains(_librarySearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(_libraryTagFilter) &&
                !model.CategoryTags.Contains(_libraryTagFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ImGui.PushID(model.Guid);

            bool nodeOpen = ImGui.TreeNodeEx($"{model.Name} [{typeName}]###ModelNode_{model.Guid}", ImGuiTreeNodeFlags.FramePadding);

            if (nodeOpen)
            {
                ImGui.TextDisabled($"GUID: {model.Guid}");
                ImGui.TextDisabled($"Tags: {model.CategoryTags}");

                if (!_modelNameDrafts.TryGetValue(model.Guid, out var renameDraft))
                {
                    renameDraft = model.Name;
                    _modelNameDrafts[model.Guid] = renameDraft;
                }

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("ModelName");
                ImGui.SameLine();
                ImGui.TextDisabled("|");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(220f);
                if (ImGui.InputText("##RenameModel", ref renameDraft, 64))
                {
                    renameDraft = renameDraft.Replace(' ', '_');
                    _modelNameDrafts[model.Guid] = renameDraft;
                    if (!string.IsNullOrWhiteSpace(renameDraft))
                    {
                        Library.RenameModel(model.Guid, renameDraft);
                    }
                }

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new NumVector4(0.75f, 0.15f, 0.15f, 1.0f));
                if (ImGui.Button("Delete"))
                {
                    guidToDelete = model.Guid;
                }
                ImGui.PopStyleColor();

                if (model.Instance != null)
                {
                    ImGui.Spacing();
                    if (Draw(model.Instance))
                    {
                        Library.SaveModel(model.Guid);
                    }
                }

                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        if (guidToDelete != null)
        {
            Library.DeleteModel(guidToDelete);
        }

        ImGui.End();
    }
}