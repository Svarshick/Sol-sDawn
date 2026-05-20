using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public interface IAnimation : IDrawable
{
    public bool IsFinished { get; }
    public void Cancel();
}

public interface IAnimationPlayer : IDrawable
{
    public void TryPlay(string animationName);
    public Vector2 Position { get; set; }
}
