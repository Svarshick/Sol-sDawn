namespace SolsDawn.Core.Logic.Animations;

public abstract class AnimationPlayer(string defaultAnimation) : IDrawable
{
    public readonly string DefaultAnimation = defaultAnimation;
    public Transform Transform;
    public abstract void Draw();
    public abstract void TryPlay(string animationName);
}

public class Animator<T> : Component<Animator<T>>, IDrawable
    where T : AnimationPlayer
{
    public readonly T Player;
    public Animator(GameObject go, T player) : base(go)
    {
        Player = player;
        Player.Transform = GameObject.Transform;
    }
    
    public override void Dispose()
    {
    }

    public void Draw()
    {
        Player.Draw();   
    }
}