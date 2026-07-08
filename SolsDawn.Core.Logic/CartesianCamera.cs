using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Collision;

namespace SolsDawn.Core.Logic;

public class CartesianCamera
{
    private readonly GraphicsDevice _graphicsDevice;

    public CartesianCamera(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        MinimumZoom = 0.1f;
        MaximumZoom = 1000f;
        Rotation = 0f;
        Zoom = 1;
        PPU = 100;
    }

    public Vector2 Position { get; set; }
    public float Rotation { get; set; }

    public int PPU { get; set; }
    
    public float Zoom
    {
        get;
        set => field = MathHelper.Clamp(value, MinimumZoom, MaximumZoom);
    }

    public float MinimumZoom { get; set; }
    public float MaximumZoom { get; set; }

    public AABB BoundingBox
    {
        get
        {
            Vector2 topLeftWorld = ScreenToWorld(Vector2.Zero);
            Vector2 bottomRightWorld = ScreenToWorld(new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height));
            return new AABB(
                new Vector2(topLeftWorld.X, bottomRightWorld.Y),
                new Vector2(bottomRightWorld.X, topLeftWorld.Y)
            );
        }
    }

    public Vector2 Center => Position;

    public Vector2 TopLeft
    {
        get
        {
            var box = BoundingBox;
            return new(box.LowerBound.X, box.UpperBound.Y);
        }
    }

    public Vector2 TopRight => BoundingBox.UpperBound;

    public Vector2 BottomLeft => BoundingBox.LowerBound;

    public Vector2 BottomRight
    {
        get
        {
            var box = BoundingBox;
            return new(box.UpperBound.X, box.LowerBound.Y);
        }
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, GetViewMatrix());
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, GetInverseViewMatrix());
    }

    public Matrix GetViewMatrix()
    {
        var viewport = _graphicsDevice.Viewport;
        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0))
               * Matrix.CreateScale(new Vector3(PPU * Zoom, -(PPU * Zoom), 1)) // The negative Y scale handles the axis flip
               * Matrix.CreateTranslation(new Vector3(viewport.Width / 2f, viewport.Height / 2f, 0));
    }

    public Matrix GetInverseViewMatrix()
    {
        return Matrix.Invert(GetViewMatrix());
    }

    public BoundingFrustum GetBoundingFrustum()
    {
        Matrix viewMatrix = GetViewMatrix();
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height, 0f, -1f, 1f);
        return new BoundingFrustum(viewMatrix * projection);
    }

    public ContainmentType Contains(Vector2 point)
    {
        return BoundingBox.Contains(ref point) ? ContainmentType.Contains : ContainmentType.Disjoint;
    }

    public ContainmentType Contains(AABB other)
    {
        AABB cameraBounds = BoundingBox;

        if (cameraBounds.Contains(ref other))
            return ContainmentType.Contains;

        if (AABB.TestOverlap(ref cameraBounds, ref other))
            return ContainmentType.Intersects;

        return ContainmentType.Disjoint;
    }
}