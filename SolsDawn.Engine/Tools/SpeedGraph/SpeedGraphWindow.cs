using System;
using System.Linq;
using ImGuiNET;
using NumVector2 = System.Numerics.Vector2;
using NumVector4 = System.Numerics.Vector4;

namespace SolsDawn.Engine;

internal static class SpeedGraphWindow
{
    private enum Mode
    {
        Live,
        View,
        List
    }

    private enum Drag
    {
        None,
        Pan,
        Band,
        BandStart,
        BandEnd
    }

    private const float ToggleWidth = 60f;
    private const float LabelWidth = 52f;
    private const float TimeLabelHeight = 16f;
    private const float OverviewHeight = 36f;
    private const float EdgeGrab = 4f;
    private const float DotSpacing = 6f;

    private static readonly uint Background = Color(0.11f, 0.13f, 0.16f, 1f);
    private static readonly uint MinorLine = Color(0.24f, 0.27f, 0.32f, 1f);
    private static readonly uint MajorLine = Color(0.40f, 0.45f, 0.52f, 1f);
    private static readonly uint ZeroLine = Color(0.60f, 0.64f, 0.70f, 1f);
    private static readonly uint LabelColor = Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly uint SeriesColor = Color(0.45f, 0.75f, 0.95f, 1f);
    private static readonly uint CursorColor = Color(0.95f, 0.85f, 0.45f, 0.8f);
    private static readonly uint BandColor = Color(0.45f, 0.75f, 0.95f, 0.18f);
    private static readonly uint BandEdgeColor = Color(0.45f, 0.75f, 0.95f, 0.8f);
    private static readonly NumVector4 DeleteColor = new(0.55f, 0.18f, 0.20f, 1f);

    private static Mode _mode = Mode.Live;
    private static SavedGraph? _selected;
    private static SavedGraph? _viewed;
    private static readonly GraphView LiveView = new(10L * SpeedRecording.FramesPerSecond);
    private static readonly GraphView SavedView = new(GraphView.MinPeriod);

    private static ValueMode _valueMode = ValueMode.Total;
    private static bool _absolute;
    private static float _axisField;
    private static float _periodDraft;
    private static bool _periodEditing;

    private static SavedGraph? _renaming;
    private static string _renameDraft = string.Empty;
    private static bool _renameFocusPending;

    private static Drag _drag;
    private static long _dragStart;
    private static long _dragEnd;
    private static double _dragFramesPerPixel;

    public static void Draw(ref bool isOpen)
    {
        if (!isOpen)
            return;

        ImGui.SetNextWindowSize(new NumVector2(760, 440), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Speed Graph", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.End();
            return;
        }

        if (_selected != null && !SpeedGraphs.Graphs.Contains(_selected))
            _selected = null;

        if (ImGui.Button(_mode == Mode.List ? "Graph" : "List", new NumVector2(ToggleWidth, 0)))
            _mode = _mode == Mode.List ? (_selected == null ? Mode.Live : Mode.View) : Mode.List;

        switch (_mode)
        {
            case Mode.Live:
                DrawLive();
                break;
            case Mode.View when _selected != null:
                DrawView(_selected);
                break;
            case Mode.View:
                _mode = Mode.Live;
                break;
            case Mode.List:
                DrawList();
                break;
        }

        ImGui.End();
    }

    private static void DrawLive()
    {
        var recording = SpeedGraphs.Recording;
        LiveView.Sync(recording, SpeedRecording.Capacity);

        ImGui.SameLine();
        if (ImGui.Button(recording.IsStopped ? "Resume" : "Stop", new NumVector2(60, 0)))
            recording.IsStopped = !recording.IsStopped;

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            recording.Clear();
            LiveView.Sync(recording, SpeedRecording.Capacity);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!SavedGraph.TryClip(recording, LiveView.Start, LiveView.End, out _, out _));
        if (ImGui.Button("Save"))
            SpeedGraphs.Add(recording, LiveView.Start, LiveView.End);
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(LiveView.Following ? "following" : "");

        DrawControls(LiveView, recording);
        DrawPlot(LiveView, recording, SpeedRecording.Capacity);
    }

    private static void DrawView(SavedGraph graph)
    {
        var maxPeriod = Math.Max(GraphView.MinPeriod, graph.Last);
        if (_viewed != graph)
        {
            _viewed = graph;
            SavedView.ShowAll(graph, maxPeriod);
        }

        SavedView.Sync(graph, maxPeriod);

        ImGui.SameLine();
        ImGui.BeginDisabled(!SpeedGraphs.CanCrop(graph, SavedView.Start, SavedView.End));
        if (ImGui.Button("Crop") && SpeedGraphs.Crop(graph, SavedView.Start, SavedView.End))
            SavedView.ShowAll(graph, Math.Max(GraphView.MinPeriod, graph.Last));
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (_renaming == graph)
        {
            ImGui.SetNextItemWidth(220f);
            DrawRename();
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(graph.Name);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                BeginRename(graph);
        }

        DrawControls(SavedView, graph);
        DrawPlot(SavedView, graph, Math.Max(GraphView.MinPeriod, graph.Last));
    }

    private static void DrawList()
    {
        ImGui.Separator();
        if (ImGui.Selectable("Live", _selected == null))
            _selected = null;

        SavedGraph? toDelete = null;
        foreach (var graph in SpeedGraphs.Graphs.OrderByDescending(g => g.CreatedTicks))
        {
            ImGui.PushID(graph.Id);
            var rowStart = ImGui.GetCursorPosX();
            var rowWidth = ImGui.GetContentRegionAvail().X - 110f;

            if (_renaming == graph)
            {
                ImGui.SetNextItemWidth(rowWidth);
                DrawRename();
            }
            else
            {
                if (ImGui.Selectable(graph.Name, _selected == graph, ImGuiSelectableFlags.AllowDoubleClick, new NumVector2(rowWidth, 0)))
                    _selected = graph;
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    BeginRename(graph);
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"{graph.DurationSeconds:F2} s");

            ImGui.SameLine(rowStart + rowWidth + 80f);
            ImGui.PushStyleColor(ImGuiCol.Button, DeleteColor);
            if (ImGui.SmallButton("X"))
                toDelete = graph;
            ImGui.PopStyleColor();

            ImGui.PopID();
        }

        if (toDelete != null)
        {
            SpeedGraphs.Delete(toDelete);
            if (_selected == toDelete)
                _selected = null;
            if (_renaming == toDelete)
                _renaming = null;
        }
    }

    private static void BeginRename(SavedGraph graph)
    {
        _renaming = graph;
        _renameDraft = graph.Name;
        _renameFocusPending = true;
    }

    private static void DrawRename()
    {
        if (_renameFocusPending)
        {
            ImGui.SetKeyboardFocusHere();
            _renameFocusPending = false;
        }

        ImGui.InputText("##RenameGraph", ref _renameDraft, 64, ImGuiInputTextFlags.AutoSelectAll);
        if (ImGui.IsItemDeactivated())
        {
            if (!ImGui.IsKeyPressed(ImGuiKey.Escape))
                SpeedGraphs.Rename(_renaming!, _renameDraft);
            _renaming = null;
        }
    }

    private static void DrawControls(GraphView view, ISpeedSamples samples)
    {
        var frameRate = samples.FrameRate;
        if (!_periodEditing)
            _periodDraft = (float)view.Period / frameRate;

        ImGui.SetNextItemWidth(80f);
        ImGui.InputFloat("##Period", ref _periodDraft, 0f, 0f, "%.2f");
        _periodEditing = ImGui.IsItemActive();
        if (ImGui.IsItemDeactivatedAfterEdit() && float.IsFinite(_periodDraft))
            view.SetPeriod((long)Math.Round(Math.Clamp(_periodDraft * frameRate, 0f, long.MaxValue / 2f)));
        ImGui.SameLine();
        ImGui.TextDisabled($"s · {view.Period} frames");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(70f);
        if (ImGui.InputFloat("Axis max", ref _axisField, 0f, 0f, "%.2f"))
            _axisField = float.IsFinite(_axisField) ? Math.Max(0f, _axisField) : 0f;

        ImGui.SameLine();
        if (ImGui.RadioButton("Total", _valueMode == ValueMode.Total))
            _valueMode = ValueMode.Total;
        ImGui.SameLine();
        if (ImGui.RadioButton("Horizontal", _valueMode == ValueMode.Horizontal))
            _valueMode = ValueMode.Horizontal;
        ImGui.SameLine();
        if (ImGui.RadioButton("Vertical", _valueMode == ValueMode.Vertical))
            _valueMode = ValueMode.Vertical;

        ImGui.SameLine();
        ImGui.BeginDisabled(_valueMode == ValueMode.Total);
        ImGui.Checkbox("Absolute", ref _absolute);
        ImGui.EndDisabled();
    }

    private static void DrawPlot(GraphView view, ISpeedSamples samples, long maxPeriod)
    {
        var io = ImGui.GetIO();
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var plotHeight = Math.Max(60f, avail.Y - OverviewHeight - TimeLabelHeight - spacing * 2f);
        var width = Math.Max(120f, avail.X);

        ImGui.InvisibleButton("##Plot", new NumVector2(width, plotHeight + TimeLabelHeight));
        var hovered = ImGui.IsItemHovered();
        var activated = ImGui.IsItemActivated();
        var active = ImGui.IsItemActive();

        var min = new NumVector2(origin.X + LabelWidth, origin.Y);
        var max = new NumVector2(origin.X + width, origin.Y + plotHeight);
        var plotWidth = max.X - min.X;

        if (hovered && io.MouseWheel != 0f)
            view.Zoom(io.MouseWheel, FrameAt(view, min.X, plotWidth, io.MousePos.X));

        if (activated)
        {
            _drag = Drag.Pan;
            _dragEnd = view.End;
            _dragFramesPerPixel = view.Period / plotWidth;
        }

        if (active && _drag == Drag.Pan && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).X;
            view.MoveTo(_dragEnd - (long)Math.Round(delta * _dragFramesPerPixel));
        }

        var signed = GraphMath.IsSigned(_valueMode, _absolute);
        var axisMax = GraphMath.AxisMax(samples, view.Start, view.End, _valueMode, _absolute, _axisField);
        var low = signed ? -axisMax : 0f;

        drawList.AddRectFilled(min, max, Background);
        DrawSpeedGrid(drawList, min, max, low, axisMax, signed);
        DrawTimeGrid(drawList, view, samples.FrameRate, min, max);

        drawList.PushClipRect(min, max, true);
        DrawSeries(drawList, samples, view.Start, view.End, min, max, low, axisMax, true);
        if (hovered && io.MousePos.X >= min.X && io.MousePos.Y <= max.Y)
            DrawCursor(drawList, view, samples, min, max);
        drawList.PopClipRect();

        DrawOverview(view, samples);
    }

    private static double FrameAt(GraphView view, float left, float width, float x) =>
        view.Start + (x - left) * view.Period / width - 0.5;

    private static float XAt(GraphView view, float left, float width, double frame) =>
        left + (float)((frame - view.Start + 0.5) * width / view.Period);

    private static float YAt(float value, NumVector2 min, NumVector2 max, float low, float high) =>
        max.Y - (value - low) / (high - low) * (max.Y - min.Y);

    private static void DrawSpeedGrid(ImDrawListPtr drawList, NumVector2 min, NumVector2 max, float low, float high, bool signed)
    {
        var step = GraphMath.SpeedStep(high - low, max.Y - min.Y);
        for (var k = (long)Math.Ceiling(low / step); k * step <= high; k++)
        {
            var value = k * step;
            var y = YAt(value, min, max, low, high);
            drawList.AddLine(new NumVector2(min.X, y), new NumVector2(max.X, y), signed && k == 0 ? ZeroLine : MinorLine);
            var label = $"{value:0.##}";
            var size = ImGui.CalcTextSize(label);
            drawList.AddText(new NumVector2(min.X - size.X - 4f, y - size.Y / 2f), LabelColor, label);
        }
    }

    private static void DrawTimeGrid(ImDrawListPtr drawList, GraphView view, int frameRate, NumVector2 min, NumVector2 max)
    {
        var width = max.X - min.X;
        var step = GraphMath.FrameStep(view.Period, width, frameRate);
        var major = GraphMath.MajorStep(step, view.Period, width, frameRate);
        var first = Math.Max(0, (long)Math.Ceiling((double)view.Start / step) * step);

        for (var frame = first; frame < view.End; frame += step)
        {
            var x = XAt(view, min.X, width, frame);
            var isMajor = frame % major == 0;
            drawList.AddLine(new NumVector2(x, min.Y), new NumVector2(x, max.Y), isMajor ? MajorLine : MinorLine);
            if (!isMajor)
                continue;

            var label = $"{(double)frame / frameRate:0.##}s";
            var size = ImGui.CalcTextSize(label);
            drawList.AddText(new NumVector2(x - size.X / 2f, max.Y + 1f), LabelColor, label);
        }
    }

    private static void DrawSeries(
        ImDrawListPtr drawList,
        ISpeedSamples samples,
        long start,
        long end,
        NumVector2 min,
        NumVector2 max,
        float low,
        float high,
        bool dots)
    {
        var width = max.X - min.X;
        var period = end - start;
        var framesPerPixel = period / (double)width;
        var from = Math.Max(start, samples.First);
        var to = Math.Min(end, samples.Last);

        float X(double frame) => min.X + (float)((frame - start + 0.5) * width / period);
        float Y(float value) => YAt(Math.Clamp(value, low, high), min, max, low, high);

        if (framesPerPixel <= 1.0)
        {
            var drawDots = dots && width / period >= DotSpacing;
            NumVector2? previous = null;
            for (var frame = from; frame < to; frame++)
            {
                if (!samples.TryGet(frame, out var velocity))
                {
                    previous = null;
                    continue;
                }

                var point = new NumVector2(X(frame), Y(GraphMath.Shown(velocity, _valueMode, _absolute)));
                if (previous is { } p)
                    drawList.AddLine(p, point, SeriesColor, 1.5f);
                if (drawDots)
                    drawList.AddCircleFilled(point, 2.5f, SeriesColor);
                previous = point;
            }

            return;
        }

        var columns = (int)Math.Ceiling(width);
        float? lastY = null;
        for (var column = 0; column < columns; column++)
        {
            var f0 = Math.Max(from, start + (long)Math.Floor(column * framesPerPixel));
            var f1 = Math.Min(to, start + (long)Math.Floor((column + 1) * framesPerPixel));
            float? firstValue = null;
            float lastValue = 0f, lowest = 0f, highest = 0f;
            for (var frame = f0; frame < f1; frame++)
            {
                if (!samples.TryGet(frame, out var velocity))
                {
                    if (firstValue != null)
                        break;
                    continue;
                }

                var value = Y(GraphMath.Shown(velocity, _valueMode, _absolute));
                if (firstValue == null)
                {
                    firstValue = value;
                    lowest = highest = value;
                }

                lowest = Math.Min(lowest, value);
                highest = Math.Max(highest, value);
                lastValue = value;
            }

            if (firstValue is not { } firstY)
            {
                lastY = null;
                continue;
            }

            var x = min.X + column + 0.5f;
            if (lastY is { } previousY)
                drawList.AddLine(new NumVector2(x - 1f, previousY), new NumVector2(x, firstY), SeriesColor, 1.5f);
            drawList.AddLine(new NumVector2(x, lowest), new NumVector2(x, highest + 0.01f), SeriesColor, 1.5f);
            lastY = lastValue;
        }
    }

    private static void DrawCursor(ImDrawListPtr drawList, GraphView view, ISpeedSamples samples, NumVector2 min, NumVector2 max)
    {
        var width = max.X - min.X;
        var frame = (long)Math.Round(FrameAt(view, min.X, width, ImGui.GetIO().MousePos.X));
        frame = Math.Clamp(frame, view.Start, view.End - 1);
        if (!samples.Contains(frame))
            return;

        var x = XAt(view, min.X, width, frame);
        drawList.AddLine(new NumVector2(x, min.Y), new NumVector2(x, max.Y), CursorColor);

        var time = (double)frame / samples.FrameRate;
        ImGui.SetTooltip(samples.TryGet(frame, out var v)
            ? $"Frame {frame}\nTime {time:F3} s\nX {v.X:F3}\nY {v.Y:F3}\n|v| {v.Length():F3}"
            : $"Frame {frame}\nTime {time:F3} s\ngap");
    }

    private static void DrawOverview(GraphView view, ISpeedSamples samples)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = Math.Max(120f, ImGui.GetContentRegionAvail().X);
        var min = new NumVector2(origin.X + LabelWidth, origin.Y);
        var max = new NumVector2(origin.X + width, origin.Y + OverviewHeight);
        var stripWidth = max.X - min.X;

        var domainStart = Math.Min(samples.First, view.Start);
        var domainEnd = Math.Max(samples.Last, domainStart + GraphView.MinPeriod);
        var domain = domainEnd - domainStart;

        float X(long frame) => min.X + (float)((frame - domainStart) * stripWidth / domain);
        long FrameAtX(float x) => domainStart + (long)Math.Round((x - min.X) * domain / stripWidth);

        ImGui.SetCursorScreenPos(min);
        ImGui.InvisibleButton("##Overview", new NumVector2(stripWidth, OverviewHeight));
        var bandMin = X(view.Start);
        var bandMax = X(view.End);

        if (ImGui.IsItemActivated())
        {
            var mouse = ImGui.GetIO().MousePos.X;
            _dragFramesPerPixel = domain / stripWidth;
            if (Math.Abs(mouse - bandMin) <= EdgeGrab)
                _drag = Drag.BandStart;
            else if (Math.Abs(mouse - bandMax) <= EdgeGrab)
                _drag = Drag.BandEnd;
            else
            {
                if (mouse < bandMin || mouse > bandMax)
                    view.MoveTo(FrameAtX(mouse) + view.Period / 2);
                _drag = Drag.Band;
            }

            _dragStart = view.Start;
            _dragEnd = view.End;
        }

        if (ImGui.IsItemActive())
        {
            var delta = (long)Math.Round(ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0f).X * _dragFramesPerPixel);
            switch (_drag)
            {
                case Drag.Band:
                    view.MoveTo(_dragEnd + delta);
                    break;
                case Drag.BandStart:
                    view.ResizeStart(_dragStart + delta, _dragEnd);
                    break;
                case Drag.BandEnd:
                    view.ResizeEnd(_dragStart, _dragEnd + delta);
                    break;
            }
        }

        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
        {
            var mouse = ImGui.GetIO().MousePos.X;
            if (ImGui.IsItemActive() ? _drag is Drag.BandStart or Drag.BandEnd
                    : Math.Abs(mouse - bandMin) <= EdgeGrab || Math.Abs(mouse - bandMax) <= EdgeGrab)
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
        }

        var signed = GraphMath.IsSigned(_valueMode, _absolute);
        var high = GraphMath.AxisMax(samples, samples.First, samples.Last, _valueMode, _absolute, 0f);
        var low = signed ? -high : 0f;

        drawList.AddRectFilled(min, max, Background);
        drawList.PushClipRect(min, max, true);
        DrawSeries(drawList, samples, domainStart, domainEnd, min, max, low, high, false);
        bandMin = X(view.Start);
        bandMax = X(view.End);
        drawList.AddRectFilled(new NumVector2(bandMin, min.Y), new NumVector2(bandMax, max.Y), BandColor);
        drawList.AddLine(new NumVector2(bandMin, min.Y), new NumVector2(bandMin, max.Y), BandEdgeColor, 2f);
        drawList.AddLine(new NumVector2(bandMax, min.Y), new NumVector2(bandMax, max.Y), BandEdgeColor, 2f);
        drawList.PopClipRect();
    }

    private static uint Color(float r, float g, float b, float a) =>
        (uint)(a * 255f) << 24 | (uint)(b * 255f) << 16 | (uint)(g * 255f) << 8 | (uint)(r * 255f);
}
