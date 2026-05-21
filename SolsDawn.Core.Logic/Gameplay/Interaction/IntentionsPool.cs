using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Gameplay.Interaction;

public interface IIntention { }
public record BladeAttackIntention(GameObject Source, IReadOnlyList<GameObject> Targets, int Damage, Vector2 LookPosition) : IIntention;
public record FireAttackIntention(GameObject Source, IReadOnlyList<GameObject> Targets, int Damage, Vector2 LookPosition) : IIntention;

public class IntentionsPool
{
    private static List<IIntention> _queue = new();
    private static List<IIntention> _nextQueue = new();

    private static bool _resolving;

    public static void Add(IIntention intention)
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

    private static void ResolveLogic()
    {
        foreach (var intention in _queue)
        {
            if (intention is not BladeAttackIntention bladeAttack)
                continue;
            
            foreach (var go in bladeAttack.Targets)
            {
                var parry = go.GetComponent<Parry>();
                if (parry is null)
                    continue;
                var boss = parry.Target.GetComponent<Boss>();
                if (boss is null)
                    continue;
                if (boss.CurrentState == Boss.State.Parried)
                    continue;
                boss.BeParried();
            }
        }

        foreach (var intention in _queue)
        {
            if (intention is not BladeAttackIntention bladeAttack)
                continue;
            
            var boss = bladeAttack.Source.GetComponent<Boss>();
            if (boss is not null && boss.CurrentState != Boss.State.Parried)
            {
                boss.DoBlade(bladeAttack.LookPosition, bladeAttack.Targets);
            }

            var player = bladeAttack.Source.GetComponent<Player>();
            if (player is not null)
            {
                var doParry = false;
                foreach (var go in bladeAttack.Targets)
                {
                    boss = go.GetComponent<Boss>();
                    if (boss is not null && boss.CurrentState == Boss.State.Parried)
                        doParry = true;
                }

                if (doParry)
                {
                    player.DoParry(bladeAttack.LookPosition);
                }
                else 
                {
                    player.DoBlade(bladeAttack.LookPosition, bladeAttack.Targets);
                }
            }
        }
    }
}