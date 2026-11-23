using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using NumVector2 = System.Numerics.Vector2;
using NumVector4 = System.Numerics.Vector4;

namespace SolsDawn.Engine;

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

internal static class AutoInspector
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

    private readonly record struct PathStep(string Id, string Label);

    private sealed record SlotFocus(ModelEdit Owner, FieldInfo Field, PathStep[] Path);

    private static readonly NumVector4 ColorCurrentText        = new(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly NumVector4 ColorCurrentHoverBg     = new(0.16f, 0.29f, 0.48f, 0.85f);

    private static readonly NumVector4 ColorReferenceText      = new(0.86f, 0.88f, 0.90f, 1.00f);
    private static readonly NumVector4 ColorReferenceHoverBg   = new(0.26f, 0.36f, 0.48f, 0.55f);

    private static readonly NumVector4 ColorCopyText           = new(0.56f, 0.78f, 0.60f, 1.00f);
    private static readonly NumVector4 ColorCopyHoverBg        = new(0.28f, 0.52f, 0.34f, 0.45f);

    private static readonly NumVector4 ColorDefaultTemplateText = new(0.50f, 0.54f, 0.58f, 1.00f);

    private static readonly NumVector4 ColorRemoveBtnBg        = new(0.35f, 0.07f, 0.09f, 0.90f);
    private static readonly NumVector4 ColorRemoveBtnText      = new(0.82f, 0.42f, 0.42f, 1.00f);
    private static readonly NumVector4 ColorRemoveBtnHoverBg   = new(0.90f, 0.16f, 0.16f, 1.00f);
    private static readonly NumVector4 ColorRemoveBtnHoverText = new(1.00f, 0.95f, 0.95f, 1.00f);

    private static readonly Dictionary<Type, List<FieldMeta>> _cache = new();

    private static readonly Dictionary<uint, bool> _slotExpanded = new();
    private static readonly Dictionary<uint, string> _slotSearchQueries = new();
    private static readonly Dictionary<uint, CachedSearchState> _slotSearchCache = new();
    private static readonly Dictionary<string, string> _modelNameDrafts = new();
    private static readonly Dictionary<uint, SlotFocus> _focus = new();
    private static uint? _pendingSearchOpenSlotId;
    private static uint? _renamingSlotId;
    private static bool _renameFocusPending;
    private static string _renameDraft = string.Empty;

    private static string _librarySearchQuery = string.Empty;
    private static string _libraryTagFilter = string.Empty;

    public static Action? OnAnyValueChanged { get; set; }

    public static string FormatLabel(string rawName) => rawName;

    public static bool Draw(ModelEntry entry)
    {
        uint focusKey = ImGui.GetID($"##Focus_{entry.Guid}");
        bool changed;
        ImGui.Indent(Gutter);
        if (_focus.TryGetValue(focusKey, out var focus) && TryGetFocusedSlot(focus, out var slot))
        {
            changed = DrawFocused(focusKey, focus, slot);
        }
        else
        {
            _focus.Remove(focusKey);
            changed = DrawFields(entry.Instance!, ModelEdit.ForModel(entry), focusKey, []);
        }
        ImGui.Unindent(Gutter);

        if (changed)
            OnAnyValueChanged?.Invoke();

        return changed;
    }

    private static bool TryGetFocusedSlot(SlotFocus focus, out ICategorySlot slot)
    {
        slot = null!;
        if (!focus.Owner.TryCurrent(out var owner) || !focus.Field.DeclaringType!.IsInstanceOfType(owner))
            return false;

        if (focus.Field.GetValue(owner) is not ICategorySlot found)
            return false;

        slot = found;
        return true;
    }

    private static bool DrawFocused(uint focusKey, SlotFocus focus, ICategorySlot slot)
    {
        var path = focus.Path;
        for (int i = 0; i < path.Length - 1; i++)
            ImGui.PushID(path[i].Id);

        bool changed = DrawCategorySlot(slot, FormatPath(path), focus.Owner, focus.Field, focusKey, path, focused: true);

        for (int i = 0; i < path.Length - 1; i++)
            ImGui.PopID();

        return changed;
    }

    private static string FormatPath(PathStep[] path)
    {
        return path.Length <= 3
            ? string.Join("/", path.Select(s => s.Label))
            : $"{path[0].Label}/.../{path[^2].Label}/{path[^1].Label}";
    }

    private static List<FieldMeta> GetMetaList(Type type)
    {
        if (!_cache.TryGetValue(type, out var metaList))
        {
            metaList = BuildMetaList(type);
            _cache[type] = metaList;
        }

        return metaList;
    }

    private static bool DrawFields(object target, ModelEdit edit, uint focusKey, PathStep[] path)
    {
        bool anyChanged = false;

        foreach (var meta in GetMetaList(target.GetType()))
        {
            var field = meta.Field;
            var label = meta.Label;

            if (meta.IsCategorySlot)
            {
                if (field.GetValue(target) is ICategorySlot slot &&
                    DrawCategorySlot(slot, label, edit, field, focusKey, [..path, new PathStep(field.Name, label)], focused: false))
                    anyChanged = true;
                continue;
            }

            if (IsRegularFieldType(field.FieldType))
            {
                if (DrawRegularField(target, meta, edit))
                    anyChanged = true;
                continue;
            }

            if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                var nestedObj = field.GetValue(target);
                if (nestedObj != null)
                {
                    ImGui.PushID(field.Name);
                    uint foldId = ImGui.GetID("##container");
                    bool isExpanded = !_slotExpanded.TryGetValue(foldId, out bool stored) || stored;
                    if (DrawFoldRow(label, true, isExpanded))
                        _slotExpanded[foldId] = isExpanded = !isExpanded;

                    var guideTop = GuideTop();

                    if (isExpanded)
                    {
                        ImGui.Indent(Gutter);
                        if (DrawFields(nestedObj, edit.Container(field), focusKey, [..path, new PathStep(field.Name, label)]))
                            anyChanged = true;
                        ImGui.Unindent(Gutter);
                        DrawGuide(guideTop);
                    }
                    ImGui.PopID();
                }
            }
        }

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

    private static bool DrawRegularField(object target, FieldMeta meta, ModelEdit edit)
    {
        var field = meta.Field;
        var label = meta.Label;
        bool isReadOnly = meta.IsReadOnly;
        bool changed = false;

        ImGui.PushID(field.Name);

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(ValueColumn());
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
                edit.SetField(field, value);
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
                edit.SetField(field, value);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(bool))
        {
            var value = (bool)field.GetValue(target)!;
            if (ImGui.Checkbox("##val", ref value) && !isReadOnly)
            {
                edit.SetField(field, value);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(Vector2))
        {
            var xnaVec = (Vector2)field.GetValue(target)!;
            var numVec = new NumVector2(xnaVec.X, xnaVec.Y);
            if (ImGui.DragFloat2("##val", ref numVec, meta.Speed, meta.Min, meta.Max) && !isReadOnly)
            {
                edit.SetField(field, new Vector2(numVec.X, numVec.Y));
                changed = true;
            }
        }
        else if (field.FieldType == typeof(NumVector2))
        {
            var numVec = (NumVector2)field.GetValue(target)!;
            if (ImGui.DragFloat2("##val", ref numVec, meta.Speed, meta.Min, meta.Max) && !isReadOnly)
            {
                edit.SetField(field, numVec);
                changed = true;
            }
        }
        else if (field.FieldType == typeof(Color))
        {
            var c = (Color)field.GetValue(target)!;
            var vec4 = new NumVector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            if (ImGui.ColorEdit4("##val", ref vec4, ImGuiColorEditFlags.NoInputs) && !isReadOnly)
            {
                edit.SetField(field, new Color(vec4.X, vec4.Y, vec4.Z, vec4.W));
                changed = true;
            }
        }
        else if (field.FieldType == typeof(string))
        {
            var str = (string?)field.GetValue(target) ?? string.Empty;
            if (ImGui.InputText("##val", ref str, 128) && !isReadOnly)
            {
                edit.SetField(field, str);
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
                edit.SetField(field, parsed);
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

    private static bool DrawCategorySlot(
        ICategorySlot slot,
        string slotName,
        ModelEdit edit,
        FieldInfo field,
        uint focusKey,
        PathStep[] path,
        bool focused)
    {
        bool changed = false;
        ImGui.PushID(slot.FieldName);
        uint slotId = ImGui.GetID("##slot");

        bool foldable = slot.IsMissingModel || GetMetaList(slot.GetActiveInstanceObject().GetType()).Count > 0;
        _slotExpanded.TryGetValue(slotId, out bool isExpanded);
        isExpanded &= foldable;

        float column = ValueColumn();

        if (DrawFoldRow(slotName, foldable, isExpanded))
        {
            isExpanded = !isExpanded;
            _slotExpanded[slotId] = isExpanded;
        }

        if (ImGui.IsItemHovered())
        {
            if (focused)
                ImGui.SetTooltip($"{string.Join("/", path.Select(s => s.Label))}\nDouble-Click: Show Whole Config");
            else if (foldable)
                ImGui.SetTooltip("Click: Fold\nDouble-Click: Focus");

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if (focused)
                {
                    _focus.Remove(focusKey);
                }
                else if (foldable)
                {
                    _focus[focusKey] = new SlotFocus(edit, field, path);
                    isExpanded = true;
                    _slotExpanded[slotId] = true;
                }
            }
        }

        float rowHeight = ImGui.GetItemRectSize().Y;
        var guideTop = GuideTop();

        ImGui.SameLine(column);

        var activeEntry = slot.GetActiveModelEntry();
        bool isMissing = slot.IsMissingModel;

        if (_renamingSlotId == slotId)
        {
            DrawSlotRename(activeEntry);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Header, isMissing ? new NumVector4(0.65f, 0.12f, 0.12f, 0.85f) : new NumVector4(0.22f, 0.27f, 0.36f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, isMissing ? new NumVector4(0.75f, 0.18f, 0.18f, 0.90f) : new NumVector4(0.28f, 0.35f, 0.48f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, isMissing ? new NumVector4(0.85f, 0.22f, 0.22f, 1.00f) : new NumVector4(0.32f, 0.42f, 0.58f, 1.00f));
            ImGui.Selectable($"{activeEntry.Name}###CatHeader", true);
            ImGui.PopStyleColor(3);
            DrawArrow(ImGui.GetItemRectMax() - new NumVector2(rowHeight * 0.5f, rowHeight * 0.5f), rowHeight, true);

            bool popupOpen = ImGui.IsPopupOpen("CategorySearchPopup");
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                if (!popupOpen)
                    ImGui.SetTooltip("Click: Open Model Search\nDouble-Click: Rename Model");

                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (!isMissing && !activeEntry.IsDefault)
                        BeginRename(slotId, activeEntry.Name);
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !popupOpen)
                {
                    _pendingSearchOpenSlotId = slotId;
                    if (!_slotSearchQueries.ContainsKey(slotId))
                        _slotSearchQueries[slotId] = string.Empty;

                    _slotSearchCache.Remove(slotId);
                    ImGui.OpenPopup("CategorySearchPopup");
                }
            }
        }

        var headerMin = ImGui.GetItemRectMin();
        var headerMax = ImGui.GetItemRectMax();

        if (DrawCategorySearchPopup(slot, slotId, headerMin, headerMax, edit, field))
            changed = true;

        if (isExpanded)
        {
            ImGui.Indent(Gutter);

            if (isMissing)
            {
                ImGui.TextColored(new NumVector4(1.0f, 0.3f, 0.3f, 1.0f),
                    $"Missing Model [{slot.ModelGuid}]. Using default fallback.");
                ImGui.BeginDisabled(true);
            }

            if (DrawFields(slot.GetActiveInstanceObject(), edit.Slot(field), focusKey, path))
                changed = true;

            if (isMissing)
                ImGui.EndDisabled();

            ImGui.Unindent(Gutter);
            DrawGuide(guideTop);
            ImGui.Spacing();
        }

        ImGui.PopID();
        return changed;
    }

    private static float Gutter => ImGui.GetFrameHeight();

    private static float ValueColumn() => Math.Max(ImGui.GetWindowWidth() * 0.45f, ImGui.GetCursorPosX() + 80f);

    private static bool DrawFoldRow(string label, bool foldable, bool isExpanded)
    {
        float x = ImGui.GetCursorPosX();
        var start = ImGui.GetCursorScreenPos();
        float width = ValueColumn() - x + Gutter - ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPosX(x - Gutter);

        if (!foldable)
        {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, NumVector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, NumVector4.Zero);
        }

        bool clicked = ImGui.Selectable("###Fold", false, ImGuiSelectableFlags.None, new NumVector2(width, 0f)) && foldable;

        if (!foldable)
            ImGui.PopStyleColor(2);

        float height = ImGui.GetTextLineHeight();
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(start, ImGui.GetItemRectMax(), true);
        drawList.AddText(start, ImGui.GetColorU32(ImGuiCol.Text), label);
        drawList.PopClipRect();

        if (foldable)
            DrawArrow(start + new NumVector2(-Gutter * 0.5f, height * 0.5f), height, isExpanded != clicked);

        return clicked;
    }

    private static float GuideTop() => ImGui.GetItemRectMax().Y;

    private static void DrawGuide(float top)
    {
        var cursor = ImGui.GetCursorScreenPos();
        float x = cursor.X - Gutter * 0.5f;
        float bottom = cursor.Y - ImGui.GetStyle().ItemSpacing.Y;
        ImGui.GetWindowDrawList().AddLine(new NumVector2(x, top), new NumVector2(x, bottom), ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.4f));
    }

    private static void DrawArrow(NumVector2 center, float size, bool pointsDown)
    {
        float r = size * 0.22f;
        uint color = ImGui.GetColorU32(ImGuiCol.Text);
        var drawList = ImGui.GetWindowDrawList();

        if (pointsDown)
            drawList.AddTriangleFilled(center + new NumVector2(0f, r), center + new NumVector2(-r, -r * 0.6f), center + new NumVector2(r, -r * 0.6f), color);
        else
            drawList.AddTriangleFilled(center + new NumVector2(r, 0f), center + new NumVector2(-r * 0.6f, r), center + new NumVector2(-r * 0.6f, -r), color);
    }

    private static void BeginRename(uint slotId, string currentName)
    {
        _renamingSlotId = slotId;
        _renameDraft = currentName;
        _renameFocusPending = true;
    }

    private static void DrawSlotRename(ModelEntry entry)
    {
        if (_renameFocusPending)
        {
            ImGui.SetKeyboardFocusHere();
            _renameFocusPending = false;
        }

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##RenameSlotModel", ref _renameDraft, 64, ImGuiInputTextFlags.AutoSelectAll);

        if (ImGui.IsItemDeactivated())
        {
            Library.RenameModel(entry.Guid, _renameDraft);
            _renamingSlotId = null;
        }
    }

    private static IReadOnlyList<ModelSearchResult> GetOrRunSearch(ICategorySlot slot, uint slotId, string query, string ownerGuid)
    {
        int currentVersion = Library.Version;
        if (_slotSearchCache.TryGetValue(slotId, out var cached) &&
            cached.LibraryVersion == currentVersion &&
            string.Equals(cached.Query, query, StringComparison.Ordinal))
        {
            return cached.Results;
        }

        var results = Library.SearchModels(slot.CategoryAttribute, slot.ModelInterfaceType, query, ownerGuid);
        _slotSearchCache[slotId] = new CachedSearchState(query, Library.Version, results);
        return results;
    }

    private static bool DrawCategorySearchPopup(
        ICategorySlot slot,
        uint slotId,
        NumVector2 headerMin,
        NumVector2 headerMax,
        ModelEdit edit,
        FieldInfo field)
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
        if (ImGui.InputTextWithHint("##ModelSearchInput", "Search (e.g. has:Model had:Model default: pattern)...",
                ref query, 128))
        {
            _slotSearchQueries[slotId] = query;
            _slotSearchCache.Remove(slotId);
        }

        bool shiftHeld = ImGui.GetIO().KeyShift;
        ImGui.TextDisabled(shiftHeld
            ? "[SHIFT HELD] Owned -> Reference | Orphaned -> Copy | Default -> Reference"
            : "Owned -> Copy | Orphaned -> Reference | Default -> Reference (Hold Shift to invert)");
        ImGui.Separator();

        var results = GetOrRunSearch(slot, slotId, query, edit.Current().Entry.Guid);

        ImGui.BeginChild("##SearchResultsList", new NumVector2(0, 0), ImGuiChildFlags.None);

        if (results.Count == 0)
        {
            ImGui.TextDisabled("No matching models found.");
        }
        else
        {
            float removeBtnWidth = ImGui.CalcTextSize("[ X ]").X + ImGui.GetStyle().FramePadding.X * 2f;
            string? guidToDelete = null;

            for (int i = 0; i < results.Count; i++)
            {
                var res = results[i];
                ImGui.PushID($"{res.Guid}_{res.ParentGuid ?? "root"}_{i}");

                bool isCurrent = string.Equals(slot.GetActiveModelEntry().Guid, res.Guid, StringComparison.OrdinalIgnoreCase);

                bool willCopy = res.WillCopy(shiftHeld);

                NumVector4 textColor;
                NumVector4 hoverBgColor;

                if (res.IsDefault)
                {
                    textColor = ColorDefaultTemplateText;
                    hoverBgColor = ColorReferenceHoverBg;
                }
                else if (isCurrent)
                {
                    textColor = ColorCurrentText;
                    hoverBgColor = ColorCurrentHoverBg;
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
                        guidToDelete = res.Guid;
                    }

                    bool btnHovered = ImGui.IsItemHovered();
                    ImGui.PopStyleColor(4);

                    if (btnHovered)
                    {
                        ImGui.SetTooltip("Delete model from Library");
                    }
                }

                if (rowClicked && !removeClicked)
                {
                    var created = edit.ApplySearchResult(field, res, willCopy);
                    if (created != null)
                        BeginRename(slotId, created.Name);

                    _slotSearchCache.Remove(slotId);
                    changed = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.PopID();
            }

            if (guidToDelete != null)
            {
                Library.DeleteModel(guidToDelete);
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
            stateLabel = "Default Model";
            controlsHint = "LMB: Reference | Shift+LMB: Reference (Always Reference)";
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
            ImGui.TextDisabled("No saved models.");
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

                if (model.Guid != Library.RootGuid)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new NumVector4(0.75f, 0.15f, 0.15f, 1.0f));
                    if (ImGui.Button("Delete"))
                    {
                        guidToDelete = model.Guid;
                    }
                    ImGui.PopStyleColor();
                }

                if (model.Instance != null)
                {
                    ImGui.Spacing();
                    Draw(model);
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