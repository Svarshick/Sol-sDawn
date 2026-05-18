using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Effects;

public class EffectsPool : IUpdatable, IDrawable
{
    private List<IEffect> _effects = new();
    private List<IEffect> _effectsBuff = new();
    private List<IPassiveEffect> _passiveEffects = new();
    private List<IPassiveEffect> _passiveEffectsBuff = new();

    public void Add(IEffect effect) => _effects.Add(effect);
    public void Add(IPassiveEffect effect) => _passiveEffects.Add(effect);
    
    public void Update(GameTime gameTime)
    {
        foreach (var effect in _effects)
        {
            effect.Update(gameTime);
        }
    }

    public void LateUpdate(GameTime gameTime)
    {
        foreach (var effect in _effects)
        {
            effect.LateUpdate(gameTime);
        }

        foreach (var effect in _effects)
        {
            if (!effect.IsFinished)
                _effectsBuff.Add(effect);
        }

        _effects.Clear();
        (_effects, _effectsBuff) = (_effectsBuff, _effects);
        
        foreach (var effect in _passiveEffects)
        {
            if (!effect.IsFinished)
                _passiveEffectsBuff.Add(effect);
        }

        _passiveEffects.Clear();
        (_passiveEffects, _passiveEffectsBuff) = (_passiveEffectsBuff, _passiveEffects);
    }

    public void Draw(GameTime gameTime)
    {
        foreach (var effect in _effects)
        {
            effect.Draw(gameTime);
        }

        foreach (var effect in _passiveEffects)
        {
            effect.Draw(gameTime);
        }
    }
}