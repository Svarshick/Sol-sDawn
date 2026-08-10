using System;

namespace SolsDawn.Core.Logic;

public class ComponentNotFoundException<T> : InvalidOperationException where T : Component
{
    public ComponentNotFoundException() : base(BuildMessage())
    {
    }

    private static string BuildMessage() => $"{typeof(T)} component not found";
}

public class LogicException : InvalidOperationException
{
    public LogicException() : base(FullMessage(string.Empty))
    {
    }
    
    public LogicException(string message) : base(message)
    {
    }

    private static string FullMessage(string message)
    {
        const string prefix = "Logic violation";
        return string.IsNullOrEmpty(message) ? $"{prefix}" : $"{prefix}: {message}";
    }
}