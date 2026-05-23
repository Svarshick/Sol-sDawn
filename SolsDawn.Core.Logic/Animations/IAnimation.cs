namespace SolsDawn.Core.Logic.Animations;

public interface IAnimation : IDrawable
{
    public bool IsFinished { get; }
    public void Cancel();
}