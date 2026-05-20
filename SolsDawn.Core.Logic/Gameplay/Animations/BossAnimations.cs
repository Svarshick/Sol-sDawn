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

    public Vector2 Position { get; set; }
    
    private IAnimation _currentAnimation;
    private readonly SpriteBatch _spriteBatch;
    private readonly BossStats Stats;

    public BossAnimations(SpriteBatch spriteBatch, ScreenLayout layout)
    {
        _spriteBatch = spriteBatch;
        Stats = ConfigReader.Read(MainConfig.BossStats, layout);
        TryPlay(Idle);
    }

    public void TryPlay(string animationName)
    {
        switch (animationName)
        {
            case Idle:
                _currentAnimation = new RectangleIdleAnimation(
                    _spriteBatch,
                    Position,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color);
                break;
            case Telegraph:
                _currentAnimation = new RectangleBlinkAnimation(
                    false,
                    _spriteBatch,
                    Stats.BladeTelegraphDuration,
                    Position,
                    Stats.Width,
                    Stats.Height,
                    Stats.Color,
                    Color.White);
                break;
            case Parried:
                _currentAnimation =  new RectangleIdleAnimation(
                    _spriteBatch,
                    Position,
                    Stats.Width,
                    Stats.Height,
                    Color.White);
                break;
        }
    }

    public void Draw(GameTime gameTime)
    {
        _currentAnimation.Draw(gameTime);
    }
}