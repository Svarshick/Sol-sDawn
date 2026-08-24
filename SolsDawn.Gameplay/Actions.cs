using System;

namespace SolsDawn.Gameplay;

public static class Actions
{
    public static async Job SimpleAttack(Entity entity)
    {
        var attackPosition = Vector2.Zero;
        var attackRadius = 1;
        var warningColor = Color.Green;
        var parryWindowColor = Color.Yellow;
        var attackColor = Color.Red;
        var parriedColor = Color.White;
        var warningTime = 0.5f;
        var parryWindowTime = 0.5f;
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
        var pw = ParryWindow(
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
            },
            _ => true);

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
}