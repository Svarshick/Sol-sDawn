using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossAnimations : IAnimationPlayer
{
    public const string Idle = "Idle";
    public const string Telegraph = "Telegraph";
    public const string Parried = "Parried";
    public const string Hit = "Hit";

    public Transform Transform { get; set; }
    
    private IAnimation _baseAnimation;
    private IAnimation _overlayAnimation;
    private readonly SpriteBatch _spriteBatch;
    private readonly BossStats Stats;

    public BossAnimations(SpriteBatch spriteBatch, ScreenLayout layout)
    {
        _spriteBatch = spriteBatch;
        Stats = ConfigReader.Read(MainConfig.BossStats, layout);
    }

    public void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _baseAnimation = new RectangleIdleAnimation(
                    _spriteBatch,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color);
                break;
            case Telegraph:
                _overlayAnimation = null;
                _baseAnimation = new RectangleBlinkAnimation(
                    true,
                    _spriteBatch,
                    Stats.BladeTelegraphDuration,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color,
                    Stats.BladeTelegraphBlinkColor);
                break;
            case Parried:
                _baseAnimation = new RectangleIdleAnimation(
                    _spriteBatch,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.ParryColor);
                break;
            case Hit:
                _overlayAnimation = new RectangleBlinkAnimation(
                    true,
                    _spriteBatch,
                    Stats.HitDuration,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color,
                    Stats.HitBlinkColor);
                break;
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (_overlayAnimation is { IsFinished: false })
        {
            _overlayAnimation.Draw(gameTime);
        }
        else
        {
            _baseAnimation.Draw(gameTime);
        }
    }
}