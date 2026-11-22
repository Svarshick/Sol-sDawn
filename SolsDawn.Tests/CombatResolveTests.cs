using System.Runtime.ExceptionServices;
using Xunit;
using static SolsDawn.Engine.GameplayAPI;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SolsDawn.Tests;

[Collection("Combat")]
public sealed class CombatResolveTests
{
    private static readonly Vector2 At = new(1000, 1000);

    private static void InJob(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        var job = Scope(() =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
        });
        job.Kill();
        CombatSystem.Resolve();
        failure?.Throw();
    }

    private static async Job Scope(Action body)
    {
        body();
        await NextFrame();
    }

    private static void Resolve(int times)
    {
        for (var i = 0; i < times; i++)
            CombatSystem.Resolve();
    }

    private static Attack NewAttack(float radius, Side targets, ParryKind kind, Vector2 offset = default) =>
        new(CreateObject(At + offset), Shapes.Circle(radius), targets, kind);

    private static Parrybox NewParrybox(Side side, ParryKind accepts, Vector2 offset = default) =>
        new(CreateObject(At + offset), Shapes.Circle(1), side, accepts);

    private static Hurtbox NewHurtbox(Side side, Vector2 offset = default) =>
        new(CreateObject(At + offset), Shapes.Circle(1), side);

    [Fact]
    public void CMB17_ParryThatDestroysTheAttackBeatsTheHit() => InJob(() =>
    {
        var attack = NewAttack(1, Side.Enemy, ParryKind.Blade);
        var parrybox = NewParrybox(Side.Enemy, ParryKind.Blade);
        var hurtbox = NewHurtbox(Side.Enemy);
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);
        attack.Parried.OnFire(attack.Destroy);

        Resolve(2);

        Assert.True(parrybox.Parried.IsFired);
        Assert.Equal(0, hits);
    });

    [Fact]
    public void CMB15_CMB17_ParryThatKeepsTheAttackStillHits() => InJob(() =>
    {
        var attack = NewAttack(1, Side.Enemy, ParryKind.Blade);
        var parrybox = NewParrybox(Side.Enemy, ParryKind.Blade);
        var hurtbox = NewHurtbox(Side.Enemy);
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);

        Resolve(2);

        Assert.True(attack.Parried.IsFired);
        Assert.True(parrybox.Parried.IsFired);
        Assert.Equal(1, hits);
        Assert.Same(attack, hurtbox.LastAttack);
        Assert.Same(hurtbox, attack.LastTarget);
    });

    [Fact]
    public void CMB13_CMB14_OneAttackParryForSeveralParryboxesAttackFirst() => InJob(() =>
    {
        var attack = NewAttack(3, Side.Enemy, ParryKind.Blade);
        var first = NewParrybox(Side.Enemy, ParryKind.Blade, new Vector2(-1, 0));
        var second = NewParrybox(Side.Enemy, ParryKind.Blade, new Vector2(1, 0));
        var order = new List<string>();
        attack.Parried.OnFire(() => order.Add("attack"));
        first.Parried.OnFire(() => order.Add("first"));
        second.Parried.OnFire(() => order.Add("second"));

        Resolve(3);

        Assert.Equal(["attack", "first", "second"], order);
        Assert.Equal(2, attack.ParriedBoxes.Count);
        Assert.Same(attack, first.ParriedBy);
        Assert.Same(attack, second.ParriedBy);
    });

    [Fact]
    public void CMB11_CMB16_AttackHitsEachOverlappingHurtboxOnceOverSeveralResolves() => InJob(() =>
    {
        var attack = NewAttack(3, Side.Enemy, ParryKind.None);
        var first = NewHurtbox(Side.Enemy, new Vector2(-1, 0));
        NewHurtbox(Side.Enemy, new Vector2(1, 0));
        var attackHits = 0;
        var firstHits = 0;
        attack.Hit.OnFire(() => attackHits++);
        first.Hit.OnFire(() => firstHits++);

        Resolve(4);

        Assert.Equal(2, attackHits);
        Assert.Equal(1, firstHits);
        Assert.Equal(2, attack.AlreadyHit.Count);
    });

    [Fact]
    public void CMB13_ParryNeedsTargetSideAndAcceptedKind() => InJob(() =>
    {
        var wrongSide = NewAttack(1, Side.Player, ParryKind.Blade);
        var wrongKind = NewAttack(1, Side.Enemy, ParryKind.Fire);
        var noKind = NewAttack(1, Side.Enemy, ParryKind.None);
        var parrybox = NewParrybox(Side.Enemy, ParryKind.Blade);

        Resolve(2);

        Assert.False(parrybox.Parried.IsFired);
        Assert.False(wrongSide.Parried.IsFired);
        Assert.False(wrongKind.Parried.IsFired);
        Assert.False(noKind.Parried.IsFired);
    });

    [Fact]
    public void CMB12_AttackCreatedDuringResolveTakesPartFromTheNextResolve() => InJob(() =>
    {
        var trigger = NewAttack(1, Side.Enemy, ParryKind.None);
        NewHurtbox(Side.Enemy);
        var resolves = 0;
        var triggerAt = -1;
        var lateAt = -1;
        trigger.Hit.OnFire(() =>
        {
            triggerAt = resolves;
            var late = NewAttack(1, Side.Enemy, ParryKind.None);
            late.Hit.OnFire(() => lateAt = resolves);
        });

        for (resolves = 1; resolves <= 3; resolves++)
            CombatSystem.Resolve();

        Assert.Equal(1, triggerAt);
        Assert.Equal(2, lateAt);
    });

    [Fact]
    public void CMB17_ShapeChangedOnParryIsUsedForHitsInTheSameResolve() => InJob(() =>
    {
        var attack = NewAttack(1, Side.Enemy, ParryKind.Blade);
        NewParrybox(Side.Enemy, ParryKind.Blade);
        var hurtbox = NewHurtbox(Side.Enemy, new Vector2(2.5f, 0));
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);
        attack.Parried.OnFire(() => attack.Collider.Shape = Shapes.Circle(3));

        Resolve(1);

        Assert.True(attack.Parried.IsFired);
        Assert.Equal(1, hits);
    });
}
