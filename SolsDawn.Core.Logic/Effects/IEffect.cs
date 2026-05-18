namespace SolsDawn.Core.Logic.Effects;

public interface IPassiveEffect : IDrawable
{
    public bool IsFinished { get; }
}

public interface IEffect : IUpdatable, IDrawable
{
    public bool IsFinished { get; }
}