using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public abstract record Intention;

public record EnterStateIntention(GameObject Object, State State) : Intention;
public record ParryIntention(ParryContext Context) : Intention;
public record HitByPlayerIntention(HitByPlayerContext Context) : Intention;
public record HitByEnemyIntention(HitByEnemyContext Context) : Intention; 

public static class IntentionsPool
{
    private static List<Intention> _intentionsQueue = new();
    private static List<Intention> _lateIntentionsQueue = new();

    public static bool IsResolving { get; private set; }

    public static void AddIntention(Intention intention)
    {
        if (IsResolving)
        {
            _lateIntentionsQueue.Add(intention);
            Console.WriteLine($"[WARNING] Intention while resolving: {intention}");
        }
        else
        {
            _intentionsQueue.Add(intention);
        }
    }

    public static void Resolve()
    {
        IsResolving = true;
        ResolveLogic();
        (_intentionsQueue, _lateIntentionsQueue) = (_lateIntentionsQueue, _intentionsQueue);
        _lateIntentionsQueue.Clear();
        IsResolving = false;
    }

    public static FightBlackboard Blackboard;

    private static void ResolveLogic()
    {
        var player = Blackboard.Player;
        var playerGO = Blackboard.Player.GameObject;
        
        EnterStateIntention playerStateEnter = null;
        
        foreach (var intention in _intentionsQueue)
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
                    
                    if (parryWindow.ParryDeterminer is null || parryWindow.ParryDeterminer(context))
                    {
                        if (parryWindow.ParryExecuter is not null)
                        {
                            var parryWindowRoutine = new Routine(async () => await parryWindow.ParryExecuter(context));
                            parryWindow.Routine.StartSubroutine(parryWindowRoutine);
                        }

                        parryWindow.Parried.Fire();
                        parryWindow.Destroy();

                        if (attack.ParryExecuter is not null)
                        {
                            var attackRoutine = new Routine(async () => await attack.ParryExecuter(context));
                            attack.Routine.StartSubroutine(attackRoutine);
                        }
                    }

                    break;
                }
                case HitByPlayerIntention playerAttack:
                {
                    var context = playerAttack.Context;
                    var attack = context.Attack;
                    if(attack.HitExecuter is not null && (attack.HitDeterminer is null || attack.HitDeterminer(context)))
                    {
                        var routine = new Routine(async () => await attack.HitExecuter(context));
                        attack.Routine.StartSubroutine(routine);
                    }
                    break;
                }
                case HitByEnemyIntention enemyAttack:
                {
                    var context = enemyAttack.Context;
                    var attack = context.Attack;
                    if(attack.HitDeterminer is null || attack.HitDeterminer(context))
                    {
                        var routine = new Routine(async () => await attack.HitExecuter(context));
                        attack.Routine.StartSubroutine(routine);
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