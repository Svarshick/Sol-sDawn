using System;
using Microsoft.Xna.Framework;
using static SolsDawn.Engine.GameplayAPI;

namespace SolsDawn.Engine;

public class CameraCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Camera";
}

internal class CameraModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is CameraCategoryAttribute;

    public override string GetTagsDescription() => "Camera";
}

internal class CameraAxisCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Camera axis";
}

internal class CameraAxisModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is CameraAxisCategoryAttribute;

    public override string GetTagsDescription() => "Camera axis";
}

[CameraModel]
public record CameraSpecs
{
    [Inspect(0.01f, 0.1f, 2f)] public float Zoom = 1f;

    [CameraAxisCategory]
    public CategorySlot<CameraAxis> X = new();

    [CameraAxisCategory]
    public CategorySlot<CameraAxis> Y = new();
}

public abstract class CameraAxis : IConstrainedModel
{
    [Inspect(0.5f, 0f, 50f)] public float Trigger = 15f;
    [Inspect(0.5f, 0f, 50f)] public float Distance = 5f;

    internal abstract int Stages { get; }

    internal abstract (float Target, IMovementModel Envelope) Stage(int stage, int side, float distance);

    void IConstrainedModel.Constrain(string? editedField)
    {
        if (Trigger >= Distance)
            return;

        if (editedField == nameof(Trigger))
            Distance = Trigger;
        else
            Trigger = Distance;
    }
}

[CameraAxisModel]
[Default]
internal class Follow : CameraAxis
{
    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Movement = new();

    internal override int Stages => 1;

    internal override (float Target, IMovementModel Envelope) Stage(int stage, int side, float distance) =>
        (-side * distance, Movement.GetActiveInstance());
}

[CameraAxisModel]
internal class Forward : CameraAxis
{
    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Reach = new();

    [EnvelopeCategory]
    public CategorySlot<IMovementModel> Lead = new();

    internal override int Stages => 2;

    internal override (float Target, IMovementModel Envelope) Stage(int stage, int side, float distance) =>
        stage == 0 ? (0f, Reach.GetActiveInstance()) : (side * distance, Lead.GetActiveInstance());
}

internal struct CameraAxisState
{
    public bool Tracking;
    public int Side;
    public int Stage;
    public bool Carry;
    public int? Direction;
    public float Velocity;
    public MovementMemory Memory;
}

internal static class CameraController
{
    private static Player? _player;
    private static Vector2 _previous;
    private static CameraAxisState _x;
    private static CameraAxisState _y;

    public static void Update(CameraSpecs specs, Player? player, float dt)
    {
        Camera.Zoom = specs.Zoom;
        if (player is null)
        {
            _player = null;
            return;
        }

        var p = player.Transform.Position;
        if (player != _player)
        {
            _player = player;
            _previous = p;
            _x = default;
            _y = default;
            Camera.Position = p;
        }

        var c = Camera.Position;
        var extent = Camera.WorldSize;
        Camera.Position = new Vector2(
            Step(specs.X.GetActiveInstance(), ref _x, c.X, p.X, p.X - _previous.X, extent.X, dt),
            Step(specs.Y.GetActiveInstance(), ref _y, c.Y, p.Y, p.Y - _previous.Y, extent.Y, dt));
        _previous = p;
    }

    public static float Step(CameraAxis model, ref CameraAxisState s, float c, float p, float dp, float extent, float dt)
    {
        var tr = model.Trigger / 100f * extent;
        var fd = model.Distance / 100f * extent;
        var motion = Math.Sign(dp);

        if (s.Tracking && motion != s.Side)
            s.Carry = false;
        if (s.Carry)
            c += dp;

        var outward = Math.Sign(p - c);
        if (Math.Abs(p - c) >= tr && outward != 0 && motion == outward && !(s.Tracking && s.Carry && s.Side == outward))
        {
            s.Tracking = true;
            s.Side = outward;
            s.Stage = 0;
            s.Carry = true;
            s.Direction = null;
        }

        if (!s.Tracking)
            return c;

        if (s.Stage < model.Stages)
        {
            var (target, envelope) = model.Stage(s.Stage, s.Side, fd);
            var o = c - p;
            var d = s.Direction ??= Math.Sign(target - o);
            if (d == 0 || Math.Sign(target - o) != d)
            {
                Reach(model, ref s);
            }
            else
            {
                s.Velocity = envelope.Calculate(s.Velocity, d, dt, ref s.Memory);
                var next = o + s.Velocity * dt;
                if (Math.Sign(target - next) != d)
                {
                    next = target;
                    Reach(model, ref s);
                }

                c = p + next;
            }
        }

        if (s.Stage >= model.Stages && !s.Carry)
            s = default;

        return c;
    }

    private static void Reach(CameraAxis model, ref CameraAxisState s)
    {
        s.Stage++;
        s.Direction = null;
        if (s.Stage < model.Stages)
            return;

        s.Velocity = 0f;
        s.Memory = default;
    }
}
