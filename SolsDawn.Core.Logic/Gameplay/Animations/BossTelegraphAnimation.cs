using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossTelegraphAnimation(IAnimation bossBlinkAnimation, StarBlinkAnimation starBlinkAnimation) : IAnimation
{
    public bool IsFinished { get; private set; }
    
    public void Draw()
    {
        bossBlinkAnimation.Draw();
        starBlinkAnimation.Draw();
        IsFinished = bossBlinkAnimation.IsFinished || starBlinkAnimation.IsFinished;
    }

    public void Cancel()
    {
        bossBlinkAnimation.Cancel();
        starBlinkAnimation.Cancel();
    }
}