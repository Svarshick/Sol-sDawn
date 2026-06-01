using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public interface IAffect { }
public record DamageAffect(GameObject Source, IReadOnlyList<GameObject> Targets, int Value) : IAffect;

public static class AffectsPool
{
    private static List<IAffect> _queue = new();
    private static List<IAffect> _nextQueue = new();

    private static bool _resolving;

    public static void Add(IAffect affect)
    {
        if (_resolving)
            throw new Exception("Can't affect externally while resolving");
        _queue.Add(affect);
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
        foreach (var affect in _queue)
        {
            switch (affect)
            {
                case DamageAffect damageAffect:
                    foreach (var target in damageAffect.Targets)
                    {
                        var player = target.GetComponent<Player>();
                        player?.BeDamaged(damageAffect.Value);
                        var boss = target.GetComponent<Boss>();
                        boss?.BeDamaged(damageAffect.Value);
                    }

                    break;
            }
        }
    }
}