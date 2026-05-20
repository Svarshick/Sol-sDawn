namespace SolsDawn.Core.Logic.Effects;

public interface IPassiveEffect : IDrawable
{
    public bool IsFinished { get; }
}

public interface IEffect : IDrawable
{
    public bool IsFinished { get; }
    public void Cancel();
}