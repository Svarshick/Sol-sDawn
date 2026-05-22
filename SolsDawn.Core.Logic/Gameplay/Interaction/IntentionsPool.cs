using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolsDawn.Core.Logic.Effects;

namespace SolsDawn.Core.Logic.Gameplay.Interaction;

public record Intention(GameObject Source);
public record BladeAttackIntention(GameObject Source, IReadOnlyList<GameObject> Targets, int Damage, Vector2 LookPosition) : Intention(Source);
public record FireAttackIntention(GameObject Source, IReadOnlyList<GameObject> Targets, int Damage, Vector2 LookPosition) : Intention(Source);

public class IntentionsPool
{
    private static List<Intention> _queue = new();
    private static List<Intention> _nextQueue = new();

    private static bool _resolving;

    public static void Add(Intention intention)
    {
        if (_resolving)
            throw new Exception("Can't affect externally while resolving");
        _queue.Add(intention);
    }

    public static void Resolve()
    {
        _resolving = true;
        ResolveLogic();
        (_queue, _nextQueue) = (_nextQueue, _queue);
        _nextQueue.Clear();
        _resolving = false;
    }

    public static GameObject PlayerGO;
    public static GameObject BossGO;
    private static void ResolveLogic()
    {
        var intentionGroups = _queue.GroupBy(intention => intention.Source).ToArray();
        var playerGroup = intentionGroups.FirstOrDefault(group => group.Key.GetComponent<Player>() is not null);
        var bossGroup = intentionGroups.FirstOrDefault(group => group.Key.GetComponent<Boss>() is not null);
        
        var playerBladeAttack = playerGroup?.FirstOrDefault(intention => intention.GetType() == typeof(BladeAttackIntention)) as BladeAttackIntention;
        var playerFireAttack = playerGroup?.FirstOrDefault(intention => intention.GetType() == typeof(FireAttackIntention)) as FireAttackIntention;
        var bossBladeAttack = bossGroup?.FirstOrDefault(intention => intention.GetType() == typeof(BladeAttackIntention)) as BladeAttackIntention;
        var bossFireAttack = bossGroup?.FirstOrDefault(intention => intention.GetType() == typeof(FireAttackIntention)) as FireAttackIntention;
        var player = PlayerGO.GetComponent<Player>();
        var boss = BossGO.GetComponent<Boss>();
        if (PlayerGO is not null &&
            BossGO is not null &&
            PlayerGO == BossGO)
        {
            throw new LogicException("player and boss on the same GameObject");
        }

        if (bossBladeAttack is not null &&
            bossFireAttack is not null &&
            bossBladeAttack == bossFireAttack)
        {
            throw new LogicException("boss can't fire and blade at the same time");
        }

        var bladeParry = false;
        if (playerBladeAttack is not null)
        {
            bladeParry = playerBladeAttack.Targets.Any(go =>
            {
                var parry = go.GetComponent<Parry>();
                return parry is not null && 
                       parry.Target == BossGO &&
                       parry.Type == ParryType.Blade;
            });
        }

        var fireParry = false;
        if (playerFireAttack is not null && playerFireAttack.Targets.Contains(BossGO))
        {
            var parry = BossGO.GetComponent<Parry>();
            fireParry = parry is not null && parry.Type == ParryType.Fire;
            if (parry is not null && parry.Target != BossGO)
            {
                throw new LogicException("boss GameObject parry must point itself");
            }
        }

        if (fireParry)
        {
            var distance = (PlayerGO.Transform.Position - BossGO.Transform.Position).Length()/2;
            var direction = playerFireAttack.LookPosition - PlayerGO.Transform.Position;
            direction.Normalize();
            var collapsePoint = PlayerGO.Transform.Position + direction * distance;
            
            player!.ParryFire(collapsePoint);
            boss!.ParryFire(collapsePoint);
            Game.EffectsPool.Add(new CircleTrace(
                Math.Max(player.Stats.FireParryTraceDuration, boss.Stats.FireTraceDuration),
                collapsePoint,
                150,
                20,
                Color.Lerp(player.Stats.FireParryTraceStartColor, boss.Stats.FireParryTraceStartColor, 0.5f),
                Color.Transparent,
                150));

            if (playerBladeAttack is not null)
            {
                player.DoBlade(playerBladeAttack.LookPosition, playerBladeAttack.Targets);
            }
        }
        
        else if (bladeParry)
        {
            player!.ParryBlade(playerBladeAttack.LookPosition);
            boss!.ParryBlade();
            
            if (playerFireAttack is not null)
            {
                player.DoFire(playerFireAttack.LookPosition, playerFireAttack.Targets);
            }
        }

        else
        {
            if (playerBladeAttack is not null)
            {
                player!.DoBlade(playerBladeAttack.LookPosition, playerBladeAttack.Targets);
            }

            if (bossBladeAttack is not null)
            {
                boss!.DoBlade(bossBladeAttack.LookPosition, bossBladeAttack.Targets);
            }
            
            if (playerFireAttack is not null)
            {
                player!.DoFire(playerFireAttack.LookPosition, playerFireAttack.Targets);
            }

            if (bossFireAttack is not null)
            {
                boss!.DoFire(bossFireAttack.LookPosition, bossFireAttack.Targets);
            }
        }
    }
}