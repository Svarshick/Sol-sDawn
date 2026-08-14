using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using static SolsDawn.Core.Logic.Gameplay.Behaviour.BehaviourAPI;

namespace SolsDawn.Core.Logic.Configs;

public class Behaviour
{
    public static async Task Init()
    {
        var boss = CreateEntity();
        
        while (true)
        {
            await SimpleAttack(boss);
            await Timer(2);
        }
    }

    public static async Task SimpleAttack(Entity entity)
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

        var warningAnimation = BehaviourAPI.Animations.CircleIdle(
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
        branch.OnEnd(() =>
        {
            pw.Destroy();
        });

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

    public static async Task Slide(Entity entity, Vector2 direction, float speed, float time)
    {
        var timer = Timer(time);
        while (!timer.IsEnded)
        {
            entity.Transform.Position += direction * speed * ElapsedSeconds;
            await NextFrame();
        }
    }
    
    public static async Task AnimationTest()
    {
        var scale = 1;
        var firstColor = Color.Yellow;
        var secondColor = Color.Green;

        var animation = BehaviourAPI.Animations.CircleIdle(
            Vector2.Zero,
            3,
            firstColor,
            0);

        var ticks = 0;
        
        while (true)
        {
            var phase = (ticks / 60) % 2;
            animation.Color = phase == 0 ? firstColor : secondColor;
            ticks++;
            await NextFrame();
        }
    }
}