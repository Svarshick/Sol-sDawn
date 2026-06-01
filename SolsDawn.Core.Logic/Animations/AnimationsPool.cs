using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Animations;

public class AnimationsPool : IUpdatable, IDrawable
{
    private List<IAnimation> _animations = new();
    private List<IAnimation> _animationsBuff = new();

    public void Add(IAnimation animation) => _animations.Add(animation);
    

    public void Update()
    {
        foreach (var effect in _animations)
        {
            if (!effect.IsFinished)
                _animationsBuff.Add(effect);
        }

        _animations.Clear();
        (_animations, _animationsBuff) = (_animationsBuff, _animations);
    }
    
    public void LateUpdate()
    {
    }

    public void Draw()
    {
        foreach (var effect in _animations)
        {
            effect.Draw();
        }
    }
}