using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class BossAnimations : IAnimationPlayer
{
    public const string Idle = "Idle";
    public const string BladeTelegraph = "Telegraph";
    public const string BladeParried = "BladeParried";
    public const string FireTelegraph = "FireTelegraph";
    public const string FireParried = "FireParried";
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
            case BladeTelegraph:
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
            case BladeParried:
                _baseAnimation = new RectangleIdleAnimation(
                    _spriteBatch,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.BladeParriedColor);
                break;
            case FireTelegraph:
                _overlayAnimation = null;
                _baseAnimation = new RectangleBlinkAnimation(
                    true,
                    _spriteBatch,
                    Stats.FireTelegraphDuration,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color,
                    Stats.FireTelegraphBlinkColor);
                break;
            case FireParried:
                _baseAnimation = new RectangleIdleAnimation(
                    _spriteBatch,
                    Transform,
                    Stats.Width,
                    Stats.Height,
                    Stats.FireParriedColor);
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