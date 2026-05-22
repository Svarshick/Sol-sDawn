using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public class Animator : Component<Animator>, IDrawable
{
    private readonly IAnimationPlayer _player;
    public Animator(GameObject go, IAnimationPlayer player, string defaultAnimation) : base(go)
    {
        _player = player;
        _player.Transform = go.Transform;
        _player.TryPlay(defaultAnimation);
    }
    
    public override void Dispose()
    {
    }

    public void Draw()
    {
        _player.Draw();   
    }
    
    public void TryPlay(string animationName) => _player.TryPlay(animationName);
}