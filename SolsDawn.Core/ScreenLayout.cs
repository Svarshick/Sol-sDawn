using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;

namespace SolsDawn.Core;

public class ScreenLayout
{
    public readonly int WidthResolution = 1920;
    public readonly int HeightResolution = 1080;
    public readonly float PixelsPerUnit = 100f;

    public readonly OrthographicCamera Camera;

    public ScreenLayout(GameWindow window, GraphicsDevice graphicsDevice)
    {
        var viewportAdapter = new BoxingViewportAdapter(window, graphicsDevice, WidthResolution, HeightResolution);
        Camera = new OrthographicCamera(viewportAdapter);
    }

    public float ToPixels(float units) => units * PixelsPerUnit;
    public Vector2 ToPixels(Vector2 units) => units * PixelsPerUnit;
    public float ToUnits(float pixels) => pixels / PixelsPerUnit;
    public Vector2 ToUnits(Vector2 pixels) => pixels / PixelsPerUnit;

    public Vector2 CameraCenter() => Camera.Center;
    public Vector2 CameraTopLeft() => Camera.BoundingRectangle.TopLeft;
    public Vector2 CameraTopRight() => Camera.BoundingRectangle.TopRight;
    public Vector2 CameraBottomLeft() => Camera.BoundingRectangle.BottomLeft;
    public Vector2 CameraBottomRight() => Camera.BoundingRectangle.BottomRight;
    
    public void FollowPosition(Vector2 position) => Camera.LookAt(position);
}