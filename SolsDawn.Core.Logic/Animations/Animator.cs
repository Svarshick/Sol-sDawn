using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public abstract class AnimationPlayer(string defaultAnimation) : IDrawable, IUpdatable
{
    public readonly string DefaultAnimation = defaultAnimation;
    public Transform2 Transform;
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Draw() { }
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
        Player.TryPlay(Player.DefaultAnimation);
    }

    public override void Update() => Player.Update();
    public override void LateUpdate() => Player.LateUpdate();
    public override void Draw() => Player.Draw();   
}