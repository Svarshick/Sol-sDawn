using System;
using System.Collections.Generic;

namespace SolsDawn.Gameplay;

public static class Intentions
{
    public static void ResolveLogic(List<Intention> intentions)
    {
        var player = Main.Player;
        var playerGO = Main.Player.GameObject;
        
        EnterStateIntention playerStateEnter = null;
        
        foreach (var intention in intentions)
        {
            switch (intention)
            {
                case EnterStateIntention playerState when
                    playerState.Object == playerGO:
                {
                    if (playerStateEnter is null)
                    {
                        playerStateEnter = playerState;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] more than one state intentions for {player}");
                    }

                    break;
                }
                case ParryIntention parry:
                {
                    var context = parry.Context;
                    var parryWindow = context.ParryWindow;
                    var attack = context.Attack;
                    if (parryWindow.GameObject.IsDestroyed)
                        break;
                    
                    if (parryWindow.Determine(context))
                    {
                        parryWindow.Execute(context);
                        parryWindow.Parried.Fire();
                        parryWindow.Destroy();
                        attack.ExecuteParry(context);
                    }

                    break;
                }
                case HitByPlayerIntention playerAttack:
                {
                    var context = playerAttack.Context;
                    var attack = context.Attack;
                    if (attack.DetermineHit(context))
                    {
                        attack.ExecuteHit(context);
                    }
                    break;
                }
                case HitByEnemyIntention enemyAttack:
                {
                    var context = enemyAttack.Context;
                    var attack = context.Attack;
                    if (attack.DetermineHit(context))
                    {
                        attack.ExecuteHit(context);
                    }
                    break;
                }
            }
        }
        
        if (playerStateEnter is not null)
        {
            player.Enter(playerStateEnter.State);
        }
    }
}