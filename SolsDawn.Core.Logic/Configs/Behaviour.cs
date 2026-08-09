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
        while (true)
        {
            await SimpleAttack();
        }
    }
    
    public static async Task SimpleAttack()
    {
        var scale = 1;
        var attackPosition = Vector2.Zero;
        var attackRadius = 1;
        var warningAnimationColor = Color.Yellow;
        var parryAnimationColor = Color.White;

        while (true)
        {
            Console.WriteLine("attack started!");
            
            var warningAnimation = BehaviourAPI.Animations.CircleIdle(
                attackPosition,
                attackRadius,
                20,
                attackRadius,
                warningAnimationColor,
                0);

            var pwShape = Shapes.Circle(attackRadius);
            var pw = ParryWindow(pwShape, attackPosition, 0, null, _ => true);
            var pwT = Timer(1 * scale);
            pwT.OnFire(() => { pw.Open(); warningAnimation.Color = parryAnimationColor; });
            
            var atkT = pwT.After(1 * scale);
            
            var branch = Race(pw.Parried, atkT);
            branch.OnEnd(() => { warningAnimation.Cancel(); pw.Destroy(); });
            
            branch.OnWinner(pw.Parried).OnFire(() => Console.WriteLine("Parry"));
            branch.OnWinner(atkT).OnFire(() => Console.WriteLine("Atk"));
            branch.OnEnd(pw.Destroy);

            await branch.Finished;
            Console.WriteLine("raise ended");
            await Timer(2);
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
            20,
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