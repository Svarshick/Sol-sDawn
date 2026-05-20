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
                _currentAnimation = new IdleCircleAnimation(
                    _spriteBatch,
                    Position,
                    Stats.Radius,
                    20,
                    Stats.Color,
                    Stats.Radius);
                break;
            case Telegraph:
                _currentAnimation = new CircleBlickAnimation(
                    false,
                    _spriteBatch,
                    Stats.BladeTelegraphDuration,
                    Position,
                    Stats.Radius,
                    20,
                    Stats.Color,
                    Color.White,
                    Stats.Radius);
                break;
            case Parried:
                _currentAnimation = new IdleCircleAnimation(
                    _spriteBatch,
                    Position,
                    Stats.Radius,
                    20,
                    Color.White,
                    Stats.Radius);
                break;
        }
    }

    public void Draw(GameTime gameTime)
    {
        _currentAnimation.Draw(gameTime);
    }
}