using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Animations;

public class PlayerAnimations : IAnimationPlayer
{
    public const string Idle = "Idle";
    public const string Hit = "Hit";

    public Transform Transform { get; set; }

    private IAnimation _baseAnimation;
    private IAnimation? _overlayAnimation;
    private readonly SpriteBatch _spriteBatch;
    private readonly PlayerStats Stats;

    public PlayerAnimations(SpriteBatch spriteBatch, ScreenLayout layout)
    {
        _spriteBatch = spriteBatch;
        Stats = ConfigReader.Read(MainConfig.PlayerStats, layout);
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
            case Hit:
                _overlayAnimation = new RectangleBlinkAnimation(
                    true,
                    _spriteBatch,
                    Stats.HitInvulnerabilityDuration,
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