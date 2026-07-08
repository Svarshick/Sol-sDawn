using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public record Intention(GameObject Source);

public record EnterStateIntention(GameObject Source, State State) : Intention(Source);
//public record ExitStateIntention(GameObject Source, State State) : Intention(Source);

public static class IntentionsPool
{
    private static List<Intention> _intentionsQueue = new();
    private static List<Intention> _lateIntentionsQueue = new();

    private static bool _resolving;

    public static void AddIntention(Intention intention)
    {
        if (_resolving)
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
        _resolving = true;
        ResolveLogic();
        (_intentionsQueue, _lateIntentionsQueue) = (_lateIntentionsQueue, _intentionsQueue);
        _lateIntentionsQueue.Clear();
        _resolving = false;
    }

    public static FightBlackboard Blackboard;

    private static void ResolveLogic()
    {
        var player = Blackboard.Player;
        var playerGO = Blackboard.Player.GameObject;
        
        EnterStateIntention playerStateEnter = null;
        var playerStateHandled = false;

        foreach (var intention in _intentionsQueue)
        {
            if (intention.Source == playerGO &&
                intention is EnterStateIntention playerState)
            {
                if (playerStateEnter is null)
                {
                    playerStateEnter = playerState;
                }
                else
                {
                    Console.WriteLine($"[WARNING] more than one state intentions for {player}");
                }
            }
        }

        if (playerStateEnter is not null &&
            playerStateEnter.State is Player.BladeExecuteState playerBlade) 
        {
            foreach (var target in playerBlade.Targets)
            {
                var parryWindow = target.GetComponent<ParryWindow>();
                if (parryWindow is not null &&
                    parryWindow.Type == ParryType.Blade)
                {
                    var parryPosition = (playerGO.Transform.Position + Blackboard.Boss.GameObject.Transform.Position) / 2;
                    var playerParry = new Player.BladeParryState(player, parryPosition);
                    player.Enter(playerParry);
                    playerStateHandled = true;
                    Game.LuaMain.EventToFire(parryWindow.ParriedEvent);
                    break;
                }
            }
        }
        
        if (!playerStateHandled && 
            playerStateEnter is not null)
        {
            player.Enter(playerStateEnter.State);
        }
    }
}