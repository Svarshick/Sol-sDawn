using System;
using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay.Actions;

public static class SimpleActions
{
    public static async Job FireAttack(Entity entity)
    {
        var fromPosition = Vector2.Zero;
        var toPosition = new Vector2(10, 0);
        var warningRadius = 1;
        var warningColor = Color.Yellow;
        var attackWidth = 1;
        var attackColor = Color.Red;
        var parriedColor = Color.White;
        var warningTime = 2f;
        var attackTime = 1f;
        var parriedTime = 1.5f;

        Console.WriteLine("Fire attack started");

        var warningAnimation = Animations.CircleIdle(
            fromPosition,
            warningRadius,
            warningColor,
            0);
        
        var end = Event();
        var atkBegin = Timer(warningTime);

        var pwShape = Shapes.Circle(warningRadius);
        var pw = Fight.FireParryWindow(
            null,
            pwShape,
            fromPosition,
            0,
            async context =>
            {
                Game.AnimationsPool.Add(new LineTraceAnimation(
                    fromPosition,
                    context.BumpPoint,
                    attackWidth,
                    parriedTime,
                    parriedColor));
            },
            async _ =>
            {
                var t = Timer(parriedTime);
                warningAnimation.Color = parriedColor;
                await t;
                warningAnimation.Kill();
                end.Fire();
            });

        pw.Open();

        var branch = Race(pw.Parried, atkBegin);
        branch.OnEnd(pw.Destroy);

        branch.OnWinner(pw.Parried).OnFire(() => Console.WriteLine("Parried"));
        branch.OnWinner(atkBegin).OnFire(() =>
        {
            Console.WriteLine("Fire attack");
            end.Fire();
        });

        await end;
        Console.WriteLine("Fire attack ended");
    }
    
    public static async Job BladeAttack(Entity entity)
    {
        var attackPosition = Vector2.Zero;
        var attackRadius = 1;
        var warningColor = Color.Green;
        var parryWindowColor = Color.Yellow;
        var attackColor = Color.Red;
        var parriedColor = Color.White;
        var warningTime = 2f;
        var parryWindowTime = 2f;
        var attackTime = 1f;
        var parriedTime = 1.5f;

        Console.WriteLine("Atk started");

        var warningAnimation = Animations.CircleIdle(
            attackPosition,
            attackRadius,
            warningColor,
            0);
        
        var end = Event();
        var pwBegin = Timer(warningTime);
        var atkBegin = pwBegin.After(parryWindowTime);

        var pwShape = Shapes.Circle(attackRadius);
        var pw = Fight.BladeParryWindow(
            null,
            pwShape,
            attackPosition,
            0,
            async _ =>
            {
                var t = Timer(parriedTime);
                warningAnimation.Color = parriedColor;
                await t;
                warningAnimation.Kill();
                end.Fire();
            });

        pwBegin.OnFire(() =>
        {
            pw.Open();
            warningAnimation.Color = parryWindowColor;
        });

        var branch = Race(pw.Parried, atkBegin);
        branch.OnEnd(pw.Destroy);

        branch.OnWinner(pw.Parried).OnFire(() => Console.WriteLine("Parried"));
        branch.OnWinner(atkBegin).OnFire(() =>
        {
            Console.WriteLine("Atk");
            warningAnimation.Kill();
            end.Fire();
        });

        await end;
        Console.WriteLine("Atk ended");
    }

    public static async Job SpawnOrb()
    {
        var go = CreateObject();
        var board = new OrbBoard();
        var animations = new OrbAnimations(board);
        new Orb(go, board, animations);

        float angle = 0;
        float angleVelocity = 1;
        float radius = 3;
        while (true)
        {
            var position = new Vector2(radius, 0).Rotated(angle);
            go.Transform.Position = position;
            angle += angleVelocity * ElapsedSeconds;
            await NextFrame();
        }
    }
}