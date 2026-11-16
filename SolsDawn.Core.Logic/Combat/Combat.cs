using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Collision;
using Collider = SolsDawn.Core.Logic.Gameplay.Collider;
using DebugCategory = SolsDawn.Core.Logic.Debug.Collider.Category;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Combat;

[Flags]
public enum Side
{
    Player = 1 << 0,
    Enemy = 1 << 1
}

public enum ParryKind
{
    None,
    Blade,
    Fire
}

public sealed class Hurtbox : Component
{
    public readonly Collider Collider;
    public readonly Side Side;
    public readonly EventStream Hit;
    public Attack? LastAttack;

    public Hurtbox(GameObject go, Shape shape, Side side) : base(go, true)
    {
        Side = side;
        Hit = new EventStream(CurrentJob);
        Collider = new Collider(go, shape, Layer.None, DebugCategory.Default);
        CombatSystem.Add(this);
    }

    public override void OnDestroyImmediate() => Hit.Cancel();

    public override void OnDestroyDelayed() => Transform.Parent = null;
}

public sealed class Parrybox : Component
{
    public readonly Collider Collider;
    public readonly Side Side;
    public readonly ParryKind Accepts;
    public readonly Event Parried;
    public Attack? ParriedBy;

    public Parrybox(GameObject go, Shape shape, Side side, ParryKind accepts) : base(go, true)
    {
        Side = side;
        Accepts = accepts;
        Parried = new Event(CurrentJob);
        Collider = new Collider(go, shape, Layer.None, DebugCategory.Parry);
        CombatSystem.Add(this);
    }

    public override void OnDestroyImmediate() => Parried.Cancel();

    public override void OnDestroyDelayed() => Transform.Parent = null;
}

public sealed class Attack : Component
{
    public readonly Collider Collider;
    public readonly Side Targets;
    public readonly ParryKind ParryKind;
    public readonly EventStream Hit;
    public readonly Event Parried;
    public readonly Event Resolved;
    public readonly HashSet<Hurtbox> AlreadyHit = new();
    public Hurtbox? LastTarget;
    public IReadOnlyList<Parrybox> ParriedBoxes = Array.Empty<Parrybox>();
    public Vector2? ParryPoint;
    internal bool WasResolved;

    public Attack(GameObject go, Shape shape, Side targets, ParryKind parryKind) : base(go, true)
    {
        Targets = targets;
        ParryKind = parryKind;
        Hit = new EventStream(CurrentJob);
        Parried = new Event(CurrentJob);
        Resolved = new Event(CurrentJob);
        Collider = new Collider(go, shape, Layer.None, DebugCategory.Attack);
        CombatSystem.Add(this);
    }

    public override void OnDestroyImmediate()
    {
        Hit.Cancel();
        Parried.Cancel();
        Resolved.Cancel();
    }

    public override void OnDestroyDelayed() => Transform.Parent = null;
}

public static class CombatSystem
{
    private static readonly List<Attack> Attacks = new();
    private static readonly List<Parrybox> Parryboxes = new();
    private static readonly List<Hurtbox> Hurtboxes = new();
    private static readonly List<Attack> AttacksToAdd = new();
    private static readonly List<Parrybox> ParryboxesToAdd = new();
    private static readonly List<Hurtbox> HurtboxesToAdd = new();
    private static readonly List<Attack> FirstResolved = new();

    internal static void Add(Attack attack) => AttacksToAdd.Add(attack);
    internal static void Add(Parrybox parrybox) => ParryboxesToAdd.Add(parrybox);
    internal static void Add(Hurtbox hurtbox) => HurtboxesToAdd.Add(hurtbox);

    public static void Resolve()
    {
        Merge(Attacks, AttacksToAdd);
        Merge(Parryboxes, ParryboxesToAdd);
        Merge(Hurtboxes, HurtboxesToAdd);

        ResolveParries();
        ResolveHits();

        foreach (var attack in Attacks)
        {
            if (attack.IsDestroyed || attack.WasResolved)
                continue;
            attack.WasResolved = true;
            FirstResolved.Add(attack);
        }

        Compact(Attacks);
        Compact(Parryboxes);
        Compact(Hurtboxes);

        foreach (var attack in FirstResolved)
            attack.Resolved.Fire();
        FirstResolved.Clear();
    }

    private static void ResolveParries()
    {
        for (int i = 0; i < Attacks.Count; i++)
        {
            var attack = Attacks[i];
            if (attack.IsDestroyed || attack.ParryKind == ParryKind.None || attack.Parried.IsEnded)
                continue;

            List<Parrybox>? boxes = null;
            foreach (var box in Parryboxes)
            {
                if (box.IsDestroyed || box.Parried.IsEnded)
                    continue;
                if ((attack.Targets & box.Side) == 0 || box.Accepts != attack.ParryKind)
                    continue;
                if (!Overlap(attack.Collider, box.Collider))
                    continue;
                (boxes ??= new()).Add(box);
            }

            if (boxes is null)
                continue;

            attack.ParriedBoxes = boxes;
            foreach (var box in boxes)
                box.ParriedBy = attack;

            attack.Parried.Fire();
            foreach (var box in boxes)
                box.Parried.Fire();
        }
    }

    private static void ResolveHits()
    {
        for (int i = 0; i < Attacks.Count; i++)
        {
            var attack = Attacks[i];
            for (int j = 0; j < Hurtboxes.Count && !attack.IsDestroyed; j++)
            {
                var hurtbox = Hurtboxes[j];
                if (hurtbox.IsDestroyed || (attack.Targets & hurtbox.Side) == 0 || attack.AlreadyHit.Contains(hurtbox))
                    continue;
                if (!Overlap(attack.Collider, hurtbox.Collider))
                    continue;

                attack.AlreadyHit.Add(hurtbox);
                attack.LastTarget = hurtbox;
                hurtbox.LastAttack = attack;
                attack.Hit.Fire();
                hurtbox.Hit.Fire();
            }
        }
    }

    private static bool Overlap(Collider a, Collider b)
    {
        var xfA = a.Pose;
        var xfB = b.Pose;
        return Collision.TestOverlap(a.Shape, 0, b.Shape, 0, ref xfA, ref xfB);
    }

    private static void Merge<T>(List<T> list, List<T> toAdd) where T : Component
    {
        foreach (var item in toAdd)
        {
            if (!item.IsDestroyed)
                list.Add(item);
        }
        toAdd.Clear();
    }

    private static void Compact<T>(List<T> list) where T : Component
    {
        int alive = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].IsDestroyed)
                list[alive++] = list[i];
        }
        list.RemoveRange(alive, list.Count - alive);
    }
}

public static class Effects
{
    private static Job? _owner;

    public static async Job Run()
    {
        _owner = CurrentJob;
        while (true)
            await NextFrame();
    }

    public static void Start(JobMethod method)
    {
        using (JobContext.Use(_owner ?? throw new InvalidOperationException("Effects job is not running")))
            method();
    }
}

public static class CombatMath
{
    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        var d = to - from;
        return d == Vector2.Zero ? Vector2.UnitX : Vector2.Normalize(d);
    }

    public static bool IsValidPointer(Vector2 center, Vector2 pointer) => Vector2.Distance(center, pointer) > 0.001f;

    public static Vector2 TangentProjection(Vector2 direction, float platformRotation)
    {
        var tangent = new Vector2(MathF.Cos(platformRotation), MathF.Sin(platformRotation));
        return Vector2.Dot(direction, tangent) >= 0 ? tangent : -tangent;
    }

    public static Vector2[] PentagonPoints(float length, float halfWidth, float tipLength) =>
    [
        new(0, -halfWidth),
        new(length, -halfWidth),
        new(length + tipLength, 0),
        new(length, halfWidth),
        new(0, halfWidth)
    ];

    public static PolygonShape Pentagon(float length, float halfWidth, float tipLength) =>
        Shapes.PolygonShape(PentagonPoints(length, halfWidth, tipLength));

    public static float FireParryDistance(Vector2 center, Vector2 axis, IEnumerable<Vector2> boxPositions, float distance)
    {
        var min = float.MaxValue;
        foreach (var position in boxPositions)
            min = Math.Min(min, Vector2.Dot(position - center, axis) / 2);
        return Math.Clamp(min, 0, distance);
    }
}
