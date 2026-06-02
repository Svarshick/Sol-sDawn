using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
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

    public static void Add(Intention intention)
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
        var boss = Blackboard.Boss;
        var bossGO = Blackboard.Boss.GameObject;
        
        EnterStateIntention playerStateEnter = null;
        EnterStateIntention bossStateEnter = null;
        var playerStateHandled = false;
        var bossStateHandled = false;

        foreach (var intention in _intentionsQueue)
        {
            if (intention.Source.IsDisposed)
                continue;
            
            if (intention.Source.GetComponent<Orb>() is { } orb &&
                intention is EnterStateIntention orbState)
            {
                orb.Enter(orbState.State);
            }
            
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

            if (intention.Source == bossGO &&
                intention is EnterStateIntention bossState)
            {
                if (bossStateEnter is null)
                {
                    bossStateEnter = bossState;
                }
                else
                {
                    Console.WriteLine($"[WARNING] more than one state intentions for {boss}");
                }
            }
        }

        if (playerStateEnter is not null &&
            playerStateEnter.State is Player.BladeExecuteState playerBlade &&
            boss.State is Boss.BladeTelegraphState bossTelegraphing &&
            playerBlade.Targets.Any(go =>
            {
                var parry = go.GetComponent<Parry>();
                return parry is not null &&
                       parry.Type == ParryType.Blade &&
                       parry.Target == bossGO;
            }))
        {
            var parryPosition = (playerGO.Transform.Position + bossGO.Transform.Position) / 2;

            //Console.WriteLine("[PARRY]: " + parryPosition);
            //Debug.DrawDot(parryPosition, Color.Red);

            var playerParry = new Player.BladeParryState(player, parryPosition);
            var bossParry = new Boss.BladeParriedState(boss, Blackboard, parryPosition);
            player.Enter(playerParry);
            boss.Enter(bossParry);

            Blackboard.IsBossLastBladeParried = true;
            Blackboard.IsBossLastBladeSuccess = false;
            Blackboard.IsPlayerLastBladeSuccess = true;
            bossStateHandled = true;
            playerStateHandled = true;
        }

        if (playerStateEnter is not null &&
            playerStateEnter.State is Player.FireExecuteState playerFire &&
            playerFire.Targets.Contains(bossGO) &&
            bossGO.GetComponent<Parry>() is { } parry &&
            parry.Type == ParryType.Fire &&
            parry.Target == bossGO)
        {
            var distance = (playerGO.Transform.Position - bossGO.Transform.Position).Length() / 2;
            var direction = playerFire.LookPosition - playerGO.Transform.Position;
            direction.Normalize();
            var parryPosition = playerGO.Transform.Position + direction * distance;

            var playerParry = new Player.FireParryState(player, parryPosition);
            var bossParry = new Boss.FireParriedState(boss, Blackboard, parryPosition);
            player.Enter(playerParry);
            boss.Enter(bossParry);

            var parryRadius = 150;
            
            var circle = new BoundingCircle2D(parryPosition, parryRadius);
            var shape = new CollisionShape2D(circle);
            var targets = new List<GameObject>();
            Collision.Overlap(shape, Collision.LayerName.Enemy, targets);
            foreach (var target in targets)
            {
                if (target.GetComponent<Orb>() is { } orb)
                {
                    var explode = new Orb.ExplodeState(orb);
                    orb.Enter(explode);
                }
            }
            
            Game.AnimationsPool.Add(new CircleTrace(
                new Transform { Position = parryPosition },
                parryRadius,
                20,
                150,
                Math.Max(player.Stats.FireParryTraceDuration, boss.Stats.FireTraceDuration),
                Color.Lerp(player.Stats.FireParryTraceStartColor, boss.Stats.FireParryTraceStartColor,
                    0.5f),
                Color.Transparent));

            Blackboard.IsBossLastFireParried = true;
            Blackboard.IsBossLastFireSuccess = false;
            Blackboard.IsPlayerLastFireSuccess = true;
            bossStateHandled = true;
            playerStateHandled = true;
        }


        if (!playerStateHandled && 
            playerStateEnter is not null)
        {
            switch (playerStateEnter.State)
            {
                case Player.BladeExecuteState bladeState:
                    Blackboard.IsPlayerLastBladeSuccess = bladeState.Targets.Count > 0;
                    break;
                case Player.FireExecuteState fireState:
                    Blackboard.IsPlayerLastFireSuccess = fireState.Targets.Count > 0;
                    break;
            }
            
            player.Enter(playerStateEnter.State);
        }

        if (!bossStateHandled &&
            bossStateEnter is not null)
        {
            switch (bossStateEnter.State)
            {
                case Boss.BladeExecutionState bladeState:
                    Blackboard.IsBossLastBladeSuccess = bladeState.Targets.Count > 0;
                    Blackboard.IsBossLastBladeParried = false;
                    break;
                case Boss.FireExecutionState fireState:
                    Blackboard.IsBossLastFireSuccess = fireState.Targets.Count > 0;
                    Blackboard.IsBossLastFireParried = false;
                    break;
            }
            
            boss.Enter(bossStateEnter.State);
        }
    }
}