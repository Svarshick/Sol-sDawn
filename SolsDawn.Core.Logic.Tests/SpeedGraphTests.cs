using System.Reflection;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.SpeedGraph;
using Xunit;

namespace SolsDawn.Core.Logic.Tests;

internal sealed class FakeSamples(long first, long last) : ISpeedSamples
{
    public long First { get; set; } = first;
    public long Last { get; set; } = last;
    public int FrameRate => 60;

    public bool TryGet(long frame, out Vector2 velocity)
    {
        velocity = new Vector2(frame, -frame);
        return this.Contains(frame);
    }
}

[Collection("Library")]
public sealed class SpeedGraphTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "solsdawn-graphs-" + Guid.NewGuid().ToString("N"));

    public SpeedGraphTests() => SpeedGraphs.Initialize(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static SpeedRecording Record(int count, params int[] gaps)
    {
        var recording = SpeedGraphs.Recording;
        for (var i = 0; i < count; i++)
            recording.Add(gaps.Contains(i) ? null : new Vector2(i, 2 * i));
        return recording;
    }

    private static SavedGraph AddGraph(int length = 10) => SpeedGraphs.Add(new FakeSamples(0, length), 0, length)!;

    private string[] Files() => Directory.GetFiles(_directory);

    [Fact]
    public void SPG01_MissingPlayerIsAGap()
    {
        var recording = Record(3, 1);

        Assert.True(recording.Contains(1));
        Assert.False(recording.TryGet(1, out _));
        Assert.True(recording.TryGet(2, out var velocity));
        Assert.Equal(new Vector2(2, 4), velocity);
    }

    [Fact]
    public void SPG02_RecordingKeepsLastFiveMinutes()
    {
        var recording = Record(SpeedRecording.Capacity + 10);

        Assert.Equal(18000, SpeedRecording.Capacity);
        Assert.Equal(10, recording.First);
        Assert.Equal(SpeedRecording.Capacity + 10, recording.Last);
        Assert.False(recording.Contains(9));
        Assert.True(recording.TryGet(10, out var velocity));
        Assert.Equal(10f, velocity.X);
    }

    [Fact]
    public void SPG04_StoppedRecordingAddsNoSamplesAndNoGaps()
    {
        var recording = Record(5);
        recording.IsStopped = true;
        recording.Add(Vector2.One);
        recording.Add(null);
        recording.IsStopped = false;
        recording.Add(new Vector2(7, 7));

        Assert.Equal(6, recording.Last);
        Assert.True(recording.TryGet(5, out var velocity));
        Assert.Equal(7f, velocity.X);
    }

    [Fact]
    public void SPG05_ClearRestartsFrameNumbers()
    {
        var recording = Record(50);

        recording.Clear();
        recording.Add(new Vector2(3, 3));

        Assert.Equal(0, recording.First);
        Assert.Equal(1, recording.Last);
        Assert.True(recording.TryGet(0, out var velocity));
        Assert.Equal(3f, velocity.X);
    }

    [Fact]
    public void SPG06_SPG10_SaveKeepsXAndYOfVisibleRangeWithGaps()
    {
        var recording = Record(100, 50);

        var graph = SpeedGraphs.Add(recording, 40, 60)!;

        Assert.Equal(20, graph.Last);
        Assert.Equal(40f, graph.X[0]);
        Assert.Equal(80f, graph.Y[0]);
        Assert.Null(graph.X[10]);
        Assert.Null(graph.Y[10]);
    }

    [Fact]
    public void SPG10_FramesBeforeFirstAreNotSaved()
    {
        var recording = Record(10);

        var graph = SpeedGraphs.Add(recording, -50, 10)!;

        Assert.Equal(10, graph.Last);
        Assert.Equal(0f, graph.X[0]);
    }

    [Fact]
    public void SPG10_RangeWithoutSamplesSavesNothing()
    {
        Assert.Null(SpeedGraphs.Add(SpeedGraphs.Recording, -600, 0));
        Assert.Empty(SpeedGraphs.Graphs);
    }

    [Fact]
    public void SPG11_NewGraphTakesNextNumber()
    {
        var first = AddGraph();
        var second = AddGraph();
        var third = AddGraph();
        SpeedGraphs.Delete(second);

        Assert.Equal("graph1", first.Name);
        Assert.Equal("graph3", third.Name);
        Assert.Equal("graph4", AddGraph().Name);

        SpeedGraphs.Rename(first, "Graph9");
        Assert.Equal("graph10", AddGraph().Name);
    }

    [Fact]
    public void SPG12_NamesAreUniqueAndHaveNoSpaces()
    {
        var first = AddGraph();
        var second = AddGraph();

        Assert.True(SpeedGraphs.Rename(first, " fast run "));
        Assert.Equal("fast_run", first.Name);

        Assert.False(SpeedGraphs.Rename(second, "FAST_RUN"));
        Assert.False(SpeedGraphs.Rename(second, "   "));
        Assert.Equal("graph2", second.Name);

        Assert.True(SpeedGraphs.Rename(first, "Fast_Run"));
        Assert.Equal("Fast_Run", first.Name);
    }

    [Fact]
    public void SPG13_CropKeepsVisibleRange()
    {
        var graph = AddGraph(20);

        Assert.False(SpeedGraphs.CanCrop(graph, 0, 20));
        Assert.False(SpeedGraphs.CanCrop(graph, -5, 25));
        Assert.True(SpeedGraphs.Crop(graph, 5, 10));

        Assert.Equal(5, graph.Last);
        Assert.Equal(5f, graph.X[0]);
        Assert.Equal(-9f, graph.Y[4]);
    }

    [Fact]
    public void SPG14_SPG15_GraphsAreWrittenOnlyOnSaveAndLoadBack()
    {
        var recording = Record(30, 3);
        var graph = SpeedGraphs.Add(recording, 0, 30)!;
        SpeedGraphs.Rename(graph, "jump");

        Assert.True(SpeedGraphs.HasUnsavedChanges);
        Assert.Empty(Files());

        Assert.True(SpeedGraphs.Save());
        Assert.False(SpeedGraphs.HasUnsavedChanges);
        Assert.Equal([Path.Combine(_directory, graph.Id + ".json")], Files());

        SpeedGraphs.Initialize(_directory);
        var loaded = Assert.Single(SpeedGraphs.Graphs);
        Assert.Equal(graph.Id, loaded.Id);
        Assert.Equal("jump", loaded.Name);
        Assert.Equal(60, loaded.FrameRate);
        Assert.Equal(graph.CreatedTicks, loaded.CreatedTicks);
        Assert.Equal(graph.X, loaded.X);
        Assert.Equal(graph.Y, loaded.Y);
        Assert.Null(loaded.X[3]);
    }

    [Fact]
    public void SPG15_RenameCropAndDeleteAreUnsavedChanges()
    {
        var graph = AddGraph(20);
        SpeedGraphs.Save();

        SpeedGraphs.Rename(graph, "other");
        Assert.True(SpeedGraphs.HasUnsavedChanges);
        SpeedGraphs.Save();

        SpeedGraphs.Crop(graph, 0, 5);
        Assert.True(SpeedGraphs.HasUnsavedChanges);
        SpeedGraphs.Save();

        SpeedGraphs.Delete(graph);
        Assert.True(SpeedGraphs.HasUnsavedChanges);
        Assert.Single(Files());
        SpeedGraphs.Save();
        Assert.Empty(Files());
    }

    [Fact]
    public void SPG15_RecordingIsNeverWritten()
    {
        Record(100);

        Assert.False(SpeedGraphs.HasUnsavedChanges);
        SpeedGraphs.Save();
        Assert.Empty(Files());
    }

    [Fact]
    public void SPG21_LibraryIgnoresGraphFolder()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), "solsdawn-data-" + Guid.NewGuid().ToString("N"));
        try
        {
            SpeedGraphs.Initialize(Path.Combine(dataDirectory, "Graphs"));
            AddGraph();
            SpeedGraphs.Save();

            Library.Initialize(dataDirectory);

            Assert.Empty(Library.GetAllModels());
        }
        finally
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    [Fact]
    public void SPG22_UnreadableFileIsLeftUntouched()
    {
        var path = Path.Combine(_directory, "broken.json");
        File.WriteAllText(path, "{ not json");

        SpeedGraphs.Initialize(_directory);
        SpeedGraphs.Save();

        Assert.Empty(SpeedGraphs.Graphs);
        Assert.Equal("{ not json", File.ReadAllText(path));
    }

    [Fact]
    public void SAV04_SaveLeavesNoTemporaryFile()
    {
        AddGraph();

        SpeedGraphs.Save();

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void SAV06_UninitializedStoreHasNoUnsavedChanges()
    {
        AddGraph();
        var field = typeof(SpeedGraphs).GetField("_directory", BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, null);

        Assert.False(SpeedGraphs.HasUnsavedChanges);
        Assert.True(SpeedGraphs.Save());
    }

    [Fact]
    public void SPG40_ShownValueByMode()
    {
        var velocity = new Vector2(-3, 4);

        Assert.Equal(5f, GraphMath.Shown(velocity, ValueMode.Total, false));
        Assert.Equal(-3f, GraphMath.Shown(velocity, ValueMode.Horizontal, false));
        Assert.Equal(3f, GraphMath.Shown(velocity, ValueMode.Horizontal, true));
        Assert.Equal(4f, GraphMath.Shown(velocity, ValueMode.Vertical, false));
        Assert.Equal(4f, GraphMath.Shown(-velocity, ValueMode.Vertical, true));
        Assert.False(GraphMath.IsSigned(ValueMode.Total, false));
        Assert.True(GraphMath.IsSigned(ValueMode.Vertical, false));
        Assert.False(GraphMath.IsSigned(ValueMode.Vertical, true));
    }

    [Fact]
    public void SPG41_PeriodIsAtLeastTwoFramesAndAtMostMax()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(600);
        view.Sync(samples, 800);

        view.SetPeriod(1);
        Assert.Equal(2, view.Period);

        view.SetPeriod(5000);
        Assert.Equal(800, view.Period);
    }

    [Fact]
    public void SPG42_ShowAllCoversGraph()
    {
        var graph = AddGraph(300);
        var view = new GraphView(2);

        view.ShowAll(graph, graph.Last);

        Assert.Equal(0, view.Start);
        Assert.Equal(300, view.End);
    }

    [Fact]
    public void SPG43_PeriodChangeKeepsEnd()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(100);
        view.Sync(samples, 18000);
        view.MoveTo(700);

        view.SetPeriod(300);

        Assert.Equal(700, view.End);
        Assert.Equal(400, view.Start);
    }

    [Fact]
    public void SPG44_StartBeforeFirstOnlyWhenPeriodIsLonger()
    {
        var samples = new FakeSamples(0, 100);
        var view = new GraphView(600);
        view.Sync(samples, 18000);

        Assert.Equal(100, view.End);
        Assert.Equal(-500, view.Start);

        samples.Last = 1000;
        view.Sync(samples, 18000);
        view.MoveTo(0);
        Assert.Equal(600, view.End);
        Assert.Equal(0, view.Start);
    }

    [Fact]
    public void SPG44_RangeMovesForwardWhenSamplesDrop()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(100);
        view.Sync(samples, 18000);
        view.MoveTo(200);

        samples.First = 300;
        samples.Last = 1300;
        view.Sync(samples, 18000);

        Assert.Equal(300, view.Start);
        Assert.False(view.Following);
    }

    [Fact]
    public void SPG45_FollowsWhileEndIsLast()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(100);
        view.Sync(samples, 18000);
        Assert.True(view.Following);

        samples.Last = 1010;
        view.Sync(samples, 18000);
        Assert.Equal(1010, view.End);

        view.MoveTo(900);
        samples.Last = 1020;
        view.Sync(samples, 18000);
        Assert.False(view.Following);
        Assert.Equal(900, view.End);

        view.MoveTo(5000);
        Assert.True(view.Following);
        Assert.Equal(1020, view.End);
    }

    [Fact]
    public void SPG46_ZoomKeepsFrameUnderCursor()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(100);
        view.Sync(samples, 18000);
        view.MoveTo(500);

        view.Zoom(1, 450);

        Assert.Equal(80, view.Period);
        Assert.Equal(410, view.Start);

        view.Zoom(-1, 450);
        Assert.Equal(100, view.Period);
        Assert.Equal(400, view.Start);
    }

    [Fact]
    public void SPG46_ZoomWhileFollowingKeepsEnd()
    {
        var samples = new FakeSamples(0, 1000);
        var view = new GraphView(100);
        view.Sync(samples, 18000);

        view.Zoom(-1, 950);

        Assert.Equal(125, view.Period);
        Assert.Equal(1000, view.End);
        Assert.True(view.Following);
    }

    [Fact]
    public void SPG50_AxisMaxIsFieldOrHighestShownValue()
    {
        var samples = new FakeSamples(0, 100);

        Assert.Equal(50f, GraphMath.AxisMax(samples, 0, 10, ValueMode.Horizontal, false, 50f));
        Assert.Equal(9f, GraphMath.AxisMax(samples, 0, 10, ValueMode.Vertical, false, 0f));
        Assert.Equal(1f, GraphMath.AxisMax(samples, 0, 1, ValueMode.Total, false, 0f));
        Assert.Equal(1f, GraphMath.AxisMax(samples, 200, 300, ValueMode.Total, false, 0f));
    }

    [Theory]
    [InlineData(10, 400f, 1)]
    [InlineData(600, 500f, 10)]
    [InlineData(18000, 600f, 300)]
    public void SPG61_FrameStepIsSmallestWithSixPixels(long period, float width, long expected)
    {
        Assert.Equal(expected, GraphMath.FrameStep(period, width, 60));
    }

    [Fact]
    public void SPG62_MajorStepIsMultipleOfFrameStepWithSixtyPixels()
    {
        Assert.Equal(120, GraphMath.MajorStep(10, 600, 500f, 60));
        Assert.Equal(2, GraphMath.MajorStep(1, 10, 400f, 60));
        Assert.Equal(15, GraphMath.MajorStep(3, 60, 300f, 60));
    }

    [Theory]
    [InlineData(10f, 240f, 1f)]
    [InlineData(10f, 100f, 5f)]
    [InlineData(0.5f, 240f, 0.05f)]
    [InlineData(30f, 240f, 5f)]
    public void SPG63_SpeedStepIsOneTwoOrFiveTimesPowerOfTen(float range, float height, float expected)
    {
        Assert.Equal(expected, GraphMath.SpeedStep(range, height), 4);
    }
}
