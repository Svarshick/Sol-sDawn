using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SolsDawn.Core.Gameplay;

public class Boss : IUpdatable, IDrawable
{
    private SpriteBatch _spriteBatch;
    private ScreenLayout _screenLayout;
    private Vector2 _worldPositionUnits;

    public Boss(SpriteBatch spriteBatch, ScreenLayout screenLayout)
    {
        _spriteBatch = spriteBatch;
        _screenLayout = screenLayout;
    }

    public void Update(GameTime gameTime)
    {
        _worldPositionUnits = (_screenLayout.CameraTopLeft() + _screenLayout.CameraCenter()) / 2;
    }
    
    public void LateUpdate(GameTime gameTime) {}

    public void Draw(GameTime gameTime)
    {
        // 1. Convert Unit Position back to Pixels specifically for rendering
        Vector2 renderPos = _worldPositionUnits;
        
        // 2. Boss size: 1x1 Unity-like Units
        float radiusPixels = _screenLayout.ToPixels(1f); 

        _spriteBatch.DrawCircle(
            center: renderPos, 
            radius: radiusPixels, 
            sides: 20, 
            color: Color.White, 
            thickness: radiusPixels
        );
    }
}