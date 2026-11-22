using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace SolsDawn.Engine;

internal static partial class SpeedGraphs
{
    private static readonly List<SavedGraph> _graphs = new();
    private static readonly HashSet<string> _dirty = new();
    private static readonly HashSet<string> _deleted = new();
    private static string? _directory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static SpeedRecording Recording { get; private set; } = new();
    public static IReadOnlyList<SavedGraph> Graphs => _graphs;
    public static bool HasUnsavedChanges => _directory != null && (_dirty.Count > 0 || _deleted.Count > 0);

    [GeneratedRegex("^graph([0-9]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DefaultNamePattern();

    public static void Initialize(string directory)
    {
        _directory = directory;
        _graphs.Clear();
        _dirty.Clear();
        _deleted.Clear();
        Recording = new SpeedRecording();

        Directory.CreateDirectory(directory);
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
                var graph = JsonSerializer.Deserialize<SavedGraph>(File.ReadAllText(file), JsonOptions)
                            ?? throw new InvalidDataException("empty file");
                if (graph.X.Length != graph.Y.Length)
                    throw new InvalidDataException("X and Y lengths differ");
                if (graph.FrameRate <= 0)
                    throw new InvalidDataException("frame rate must be positive");

                graph.Id = Path.GetFileNameWithoutExtension(file);
                graph.Name = UniqueLoadedName(Sanitize(graph.Name));
                _graphs.Add(graph);
            }
            catch (Exception ex)
            {
                Log.Logic.Error($"Failed to load speed graph '{file}': {ex.Message}");
            }
        }
    }

    public static void Record(Vector2? velocity) => Recording.Add(velocity);

    public static string NextName()
    {
        var max = 0;
        foreach (var graph in _graphs)
        {
            var match = DefaultNamePattern().Match(graph.Name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                max = Math.Max(max, number);
        }

        return $"graph{max + 1}";
    }

    public static SavedGraph? Add(ISpeedSamples source, long start, long end)
    {
        if (!SavedGraph.TryClip(source, start, end, out var from, out var to))
            return null;

        var graph = new SavedGraph { Name = NextName() };
        graph.SetSamples(source, from, to);
        _graphs.Add(graph);
        _dirty.Add(graph.Id);
        return graph;
    }

    public static bool CanCrop(SavedGraph graph, long start, long end) =>
        SavedGraph.TryClip(graph, start, end, out var from, out var to) && (from > graph.First || to < graph.Last);

    public static bool Crop(SavedGraph graph, long start, long end)
    {
        if (!CanCrop(graph, start, end))
            return false;

        SavedGraph.TryClip(graph, start, end, out var from, out var to);
        var copy = new SavedGraph { X = graph.X, Y = graph.Y, FrameRate = graph.FrameRate };
        graph.SetSamples(copy, from, to);
        _dirty.Add(graph.Id);
        return true;
    }

    public static bool Rename(SavedGraph graph, string name)
    {
        var sanitized = Sanitize(name);
        if (sanitized.Length == 0 || IsTaken(sanitized, graph))
            return false;

        if (sanitized == graph.Name)
            return true;

        graph.Name = sanitized;
        _dirty.Add(graph.Id);
        return true;
    }

    public static void Delete(SavedGraph graph)
    {
        if (!_graphs.Remove(graph))
            return;

        _dirty.Remove(graph.Id);
        _deleted.Add(graph.Id);
    }

    public static bool Save()
    {
        if (_directory == null)
            return true;

        var ok = true;
        Directory.CreateDirectory(_directory);

        foreach (var graph in _graphs.Where(g => _dirty.Contains(g.Id)).ToList())
        {
            try
            {
                var path = FilePath(graph.Id);
                var temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(graph, JsonOptions));
                File.Move(temporaryPath, path, overwrite: true);
                _dirty.Remove(graph.Id);
            }
            catch (Exception ex)
            {
                ok = false;
                Log.Logic.Error($"Failed to save speed graph '{graph.Name}': {ex.Message}");
            }
        }

        foreach (var id in _deleted.ToList())
        {
            try
            {
                var path = FilePath(id);
                if (File.Exists(path))
                    File.Delete(path);
                _deleted.Remove(id);
            }
            catch (Exception ex)
            {
                ok = false;
                Log.Logic.Error($"Failed to delete speed graph file '{id}': {ex.Message}");
            }
        }

        return ok;
    }

    private static string FilePath(string id) => Path.Combine(_directory!, id + ".json");

    private static string Sanitize(string? name) => (name ?? string.Empty).Trim().Replace(' ', '_');

    private static bool IsTaken(string name, SavedGraph? except) =>
        _graphs.Any(g => g != except && string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string UniqueLoadedName(string name)
    {
        if (name.Length == 0)
            name = NextName();

        var candidate = name;
        for (var i = 2; IsTaken(candidate, null); i++)
            candidate = $"{name}_{i}";

        return candidate;
    }
}
