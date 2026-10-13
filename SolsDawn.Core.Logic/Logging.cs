using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Fatal
}

public static class LogSettings
{
    public static LogLevel MinLevel = LogLevel.Info;
    public static readonly HashSet<string> DisabledTags = new HashSet<string>();
}

public class TaggedLogger
{
    private readonly string _tag;
    public TaggedLogger(string tag) => _tag = tag;

    public void Info(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
        => Log.Write(LogLevel.Info, _tag, msg, f, l);

    public void Warning(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
        => Log.Write(LogLevel.Warning, _tag, msg, f, l);

    public void Error(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
        => Log.Write(LogLevel.Error, _tag, msg, f, l);

    public void Error(Exception ex, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
        => Log.Write(LogLevel.Error, _tag, $"{ex.Message}\n{ex.StackTrace}", f, l);

    [DoesNotReturn]
    public void Fatal(string msg = "", [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
    {
        Log.Write(LogLevel.Fatal, _tag, msg, f, l);
        throw new Exception($"[FATAL] {_tag}: {msg}");
    }

    public Exception FatalException(string msg = "", [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
    {
        Log.Write(LogLevel.Fatal, _tag, msg, f, l);
        return new Exception($"[FATAL] {_tag}: {msg}");
    }
}

public static class Log
{
    public static readonly TaggedLogger Physics = new TaggedLogger(nameof(Physics));
    public static readonly TaggedLogger Graphics = new TaggedLogger(nameof(Graphics));
    public static readonly TaggedLogger Logic = new TaggedLogger(nameof(Logic));
    public static readonly TaggedLogger Job = new TaggedLogger(nameof(Core));

    internal static void Write(LogLevel level, string tag, string message, string file, int line)
    {
        if (level < LogSettings.MinLevel)
            return;
        if (!string.IsNullOrEmpty(tag) && LogSettings.DisabledTags.Contains(tag))
            return;

        long frame = 0;
        try
        {
            frame = GameplayAPI.TotalFrames;
        }
        catch
        {
            // ignored
        }

        var levelSize = 9;
        var frameSize = frame.ToString().Length;
        var tagSize = tag.Length + 2;
        var spacesCount = levelSize + 1 + frameSize + 1 + tagSize;
        var spaces = new string(' ', spacesCount);

        var codeLink = $"\n{spaces}↖ file://{file}:{line}";
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColor(level);
        Console.Write($"[{level.ToString(),-7}] {frame} [{tag}] {message}{codeLink}\n");
        Console.ForegroundColor = defaultColor;
    }

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Info => ConsoleColor.DarkGray,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };
}