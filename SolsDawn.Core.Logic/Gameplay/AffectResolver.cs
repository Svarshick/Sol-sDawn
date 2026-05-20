using System;
using System.Collections.Generic;
using System.Linq;

namespace SolsDawn.Core.Logic.Gameplay;

public enum AffectType
{
    Damage,
    Parry,
}

public record DamageArgs(int Value);
public record ParryArgs(Parry Component);

public record struct Affect(
    GameObject Source,
    GameObject Target,
    AffectType Type,
    object Args
);

public static class AffectResolver
{
    private static List<Affect> _queue = new();
    private static List<Affect> _nextQueue = new();

    private static bool _resolving;

    public static void Affect(Affect affect)
    {
        if (_resolving)
            throw new Exception("Try to affect while resolving");
        _queue.Add(affect);   
    }

    public static void Resolve()
    {
        _resolving = true;
        var groups = _queue.GroupBy(a => (a.Source, a.Target));
        foreach (var group in groups)
        {
            var source = group.Key.Source;
            var target = group.Key.Target;
            bool parried = false;
            foreach (var affect in group)
            {
                if (affect.Type != AffectType.Parry)
                    continue;
                var args = affect.Args as ParryArgs;
                var parryComponent = args!.Component;
                if (parryComponent.Target != target)
                    throw new Exception("Wrong logic");
                
                var boss = target.GetComponent<Boss>();
                if (parried || boss is null)
                    continue;
                boss.Parry();
                parried = true;
            }

            if (parried)
                continue;

            foreach (var affect in group)
            {
                if (affect.Type != AffectType.Damage)
                    continue;
                var args = affect.Args as DamageArgs;
                var hp = target.GetComponent<Hp>();
                if (hp is null)
                    continue;
                
                var player = target.GetComponent<Player>();
                if (player is not null)
                    player.Damage(args.Value);
                var boss = target.GetComponent<Boss>();
                if (boss is not null)
                    boss.Damage(args.Value);
            }
        }

        (_queue, _nextQueue) = (_nextQueue, _queue);
        _nextQueue.Clear();
        _resolving = false;
    }
}