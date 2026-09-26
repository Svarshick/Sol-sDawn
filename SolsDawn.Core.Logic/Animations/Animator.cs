using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Animations;

public abstract class AnimationSet : IDrawable, IUpdatable
{
    public const string Hit = "Hit";
    public const string Idle = "Idle";
    
    public Transform2 Transform;
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Draw() { }
    public abstract void TryPlay(string animationName);
}

public class Animator<T> : Component 
    where T : AnimationSet
{
    public readonly T Player;
    public Animator(GameObject go, T player) : base(go)
    {
        Player = player;
        Player.Transform = GameObject.Transform;
        Player.TryPlay(AnimationSet.Idle);
    }

    public override void Update() => Player.Update();
    public override void LateUpdate() => Player.LateUpdate();
    public override void Draw() => Player.Draw();   
}