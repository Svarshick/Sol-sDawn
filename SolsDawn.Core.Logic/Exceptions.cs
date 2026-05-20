using System;

namespace SolsDawn.Core.Logic;

public class ComponentNotFoundException<T> : InvalidOperationException where T : Component<T>
{
    public ComponentNotFoundException() : base(BuildMessage())
    {
    }

    public ComponentNotFoundException(Exception innerException) : base(BuildMessage(), innerException)
    {
    }

    private static string BuildMessage() => $"{typeof(T)} component not found";
}