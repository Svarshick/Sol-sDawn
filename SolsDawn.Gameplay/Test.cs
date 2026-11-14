using System.Collections.Generic;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Combat;
using SolsDawn.Gameplay.Utils;
namespace SolsDawn.Gameplay;

public static class Test
{
    public static async Job CircleCast()
    {
        var platform1 = new Platform(CreateObject(), 2);
        platform1.Transform.Position = new Vector2(2.5f, 0);
        await Timer(1);
        
        var result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform1.Collider} | {result.Fraction}"); //0.005 inaccuracy by Settings.LinearSlop
        }
        else
        {
            Log.Physics.Error("Should hit");
        }

        result = Physics.CircleCast(Vector2.Zero, new Vector2(0, 2), 1, Layer.Wall);
        if (!result.Hit)
        {
            Log.Physics.Info($"Shouldn't be any result"); //0.005 inaccuracy by Settings.LinearSlop
        }
        else
        {
            Log.Physics.Error("Shouldn't hit");
        }
        
        var platform2 = new Platform(CreateObject(), 2);
        platform2.Transform.Position = new Vector2(1.51f, 0);
        platform2.Transform.Rotation = PI / 2;
        await Timer(1);
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform1.Collider} | {result.Fraction}");
        }
        else
        {
            Log.Physics.Error($"Should hit");
        }
        
        var platform3 = new Platform(CreateObject(), 2);
        platform3.Transform.Position = new Vector2(1 / Sqrt(2), 1 / Sqrt(2));
        platform3.Transform.Rotation = -PI / 4;
        platform2.Transform.Position = new Vector2(1.1f, 0);
        await Timer(1);
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info($"{result.Collider == platform3.Collider} | {result.Fraction}");
        }
        else
        {
            Log.Physics.Error("Should hit");
        }
        
        platform1.Destroy();
        platform2.Destroy();
        platform3.Destroy();
        var platform4 = new Platform(CreateObject(), 2);
        platform4.Transform.Position = new Vector2(-0.5f, 0);
        platform4.Transform.Rotation = -PI / 2;
        await Timer(1);
        
        result = Physics.CircleCast(Vector2.Zero, new Vector2(1, 0), 1, Layer.Wall);
        if (!result.Hit)
        {
            Log.Physics.Info("Ok");
        }
        else
        {
            Log.Physics.Error("Shouldn't hit");
        }       

        while (true)
            await NextFrame();
    }

    public static async Job CircleCast2()
    {
        var platform = new Platform(CreateObject(), 2);
        var result = Physics.CircleCast(new Vector2(0, 1), Vector2.Zero, 0.5f, Layer.Wall);
        if (result.Hit)
        {
            Log.Physics.Info("OK");
        }
        else
        {
            Log.Physics.Error("Loh");
        }
    }

    public static async Job CombatResolve()
    {
        await ParryThatDestroysAttackBeatsHit();
        await ParryThatKeepsAttackStillHits();
        await OneParryForSeveralParryboxes();
        await AttackHitsEachHurtboxOnce();
        await ParryNeedsMatchingSideAndKind();
        await ObjectCreatedDuringResolveWaitsForNextResolve();
        await ShapeChangedOnParryChangesHits();
        Log.Logic.Info("Combat resolve scenarios finished");
    }

    private static void Check(bool ok, string name)
    {
        if (ok)
            Log.Logic.Info($"{name}: ok");
        else
            Log.Logic.Error($"{name}: failed");
    }

    private static Attack TestAttack(Vector2 position, float radius, Side targets, ParryKind kind) =>
        new(CreateObject(position), Shapes.Circle(radius), targets, kind);

    private static Parrybox TestParrybox(Vector2 position, Side side, ParryKind accepts) =>
        new(CreateObject(position), Shapes.Circle(1), side, accepts);

    private static Hurtbox TestHurtbox(Vector2 position, Side side) =>
        new(CreateObject(position), Shapes.Circle(1), side);

    private static async Job Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await NextFrame();
    }

    private static async Job ParryThatDestroysAttackBeatsHit()
    {
        var at = new Vector2(1000, 1000);
        var attack = TestAttack(at, 1, Side.Enemy, ParryKind.Blade);
        var parrybox = TestParrybox(at, Side.Enemy, ParryKind.Blade);
        var hurtbox = TestHurtbox(at, Side.Enemy);
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);
        attack.Parried.OnFire(attack.Destroy);

        await Frames(2);
        Check(parrybox.Parried.IsFired && hits == 0, "CMB-17 parry that destroys the attack beats the hit");
    }

    private static async Job ParryThatKeepsAttackStillHits()
    {
        var at = new Vector2(1010, 1000);
        var attack = TestAttack(at, 1, Side.Enemy, ParryKind.Blade);
        var parrybox = TestParrybox(at, Side.Enemy, ParryKind.Blade);
        var hurtbox = TestHurtbox(at, Side.Enemy);
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);

        await Frames(2);
        Check(attack.Parried.IsFired && parrybox.Parried.IsFired && hits == 1 && hurtbox.LastAttack == attack && attack.LastTarget == hurtbox,
            "CMB-15 CMB-17 parry that keeps the attack still hits");
    }

    private static async Job OneParryForSeveralParryboxes()
    {
        var at = new Vector2(1020, 1000);
        var attack = TestAttack(at, 3, Side.Enemy, ParryKind.Blade);
        var first = TestParrybox(at + new Vector2(-1, 0), Side.Enemy, ParryKind.Blade);
        var second = TestParrybox(at + new Vector2(1, 0), Side.Enemy, ParryKind.Blade);
        var order = new List<string>();
        attack.Parried.OnFire(() => order.Add("attack"));
        first.Parried.OnFire(() => order.Add("first"));
        second.Parried.OnFire(() => order.Add("second"));

        await Frames(3);
        Check(string.Join(",", order) == "attack,first,second" &&
              attack.ParriedBoxes.Count == 2 &&
              first.ParriedBy == attack && second.ParriedBy == attack,
            "CMB-13 CMB-14 one attack parry for several parryboxes, attack first");
    }

    private static async Job AttackHitsEachHurtboxOnce()
    {
        var at = new Vector2(1030, 1000);
        var attack = TestAttack(at, 3, Side.Enemy, ParryKind.None);
        var first = TestHurtbox(at + new Vector2(-1, 0), Side.Enemy);
        var second = TestHurtbox(at + new Vector2(1, 0), Side.Enemy);
        var attackHits = 0;
        var firstHits = 0;
        attack.Hit.OnFire(() => attackHits++);
        first.Hit.OnFire(() => firstHits++);

        await Frames(4);
        Check(attackHits == 2 && firstHits == 1 && attack.AlreadyHit.Count == 2,
            "CMB-11 CMB-16 attack hits each overlapping hurtbox once over several frames");
    }

    private static async Job ParryNeedsMatchingSideAndKind()
    {
        var at = new Vector2(1040, 1000);
        var wrongSide = TestAttack(at, 1, Side.Player, ParryKind.Blade);
        var wrongKind = TestAttack(at, 1, Side.Enemy, ParryKind.Fire);
        var noKind = TestAttack(at, 1, Side.Enemy, ParryKind.None);
        var parrybox = TestParrybox(at, Side.Enemy, ParryKind.Blade);

        await Frames(2);
        Check(!parrybox.Parried.IsFired && !wrongSide.Parried.IsFired && !wrongKind.Parried.IsFired && !noKind.Parried.IsFired,
            "CMB-13 parry needs a target side and the accepted kind");
    }

    private static async Job ObjectCreatedDuringResolveWaitsForNextResolve()
    {
        var at = new Vector2(1050, 1000);
        var trigger = TestAttack(at, 1, Side.Enemy, ParryKind.None);
        var hurtbox = TestHurtbox(at, Side.Enemy);
        long triggerFrame = -1;
        long lateFrame = -1;
        Attack? late = null;
        trigger.Hit.OnFire(() =>
        {
            triggerFrame = TotalFrames;
            late = TestAttack(at, 1, Side.Enemy, ParryKind.None);
            late.Hit.OnFire(() => lateFrame = TotalFrames);
        });

        await Frames(3);
        Check(late is not null && triggerFrame >= 0 && lateFrame == triggerFrame + 1,
            "CMB-12 attack created during Resolve takes part from the next Resolve");
    }

    private static async Job ShapeChangedOnParryChangesHits()
    {
        var at = new Vector2(1060, 1000);
        var attack = TestAttack(at, 1, Side.Enemy, ParryKind.Blade);
        TestParrybox(at, Side.Enemy, ParryKind.Blade);
        var hurtbox = TestHurtbox(at + new Vector2(2.5f, 0), Side.Enemy);
        var hits = 0;
        hurtbox.Hit.OnFire(() => hits++);
        attack.Parried.OnFire(() => attack.Collider.Shape = Shapes.Circle(3));

        await Frames(2);
        Check(attack.Parried.IsFired && hits == 1, "CMB-17 shape changed on parry is used for hits in the same frame");
    }
}