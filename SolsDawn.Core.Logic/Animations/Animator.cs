using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Animations;

public class Animator(GameObject go, IAnimationPlayer player) 
    : Component<Animator>(go), IDrawable
{
    public override void Dispose()
    {
    }

    public void Draw(GameTime gameTime)
    {
        player.Position = GameObject.Position;
        player.Draw(gameTime);   
    }
    
    public void TryPlay(string animationName) => player.TryPlay(animationName);
}