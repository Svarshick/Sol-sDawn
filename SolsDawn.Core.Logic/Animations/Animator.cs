using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public abstract class AnimationPlayer(string defaultAnimation) : IDrawable
{
    public readonly string DefaultAnimation = defaultAnimation;
    public Transform2 Transform;
    public abstract void Draw();
    public abstract void TryPlay(string animationName);
}

public class Animator<T> : Component 
    where T : AnimationPlayer
{
    public readonly T Player;
    public Animator(GameObject go, T player) : base(go)
    {
        Player = player;
        Player.Transform = GameObject.Transform;
    }

    public override void Draw()
    {
        Player.Draw();   
    }
}