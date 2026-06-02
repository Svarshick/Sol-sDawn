using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class OrbController : IUpdatable
{
    private static int _id = 100;
    
    private List<(Orb Orb, Func<Vector2> TargetFunc)> _orbData = new();
    private List<(Orb Orb, Func<Vector2> TargetFunc)> _orbDataBuff = new();
    private List<Orb> _removedOrbs = new();

    private bool toClear = false;
    public OrbController()
    {
        BossBehaviourBuilder.RESET += () => toClear = true;
    }
    
    public void Spawn(Vector2 position, Func<Vector2> targetFunc, OrbStats stats)
    {
        var go = new GameObject();
        go.Transform.Position = position;
        new Hp(go, 1);
        new Collider(go, _id++, Collision.LayerName.Enemy);
        var orb = new Orb(go, stats);
        _orbData.Add((orb, targetFunc));
    }
    
    public void Update()
    {
        foreach (var data in _orbData)
        {
            var orb = data.Orb;
            if (orb.GameObject.IsDisposed)
            {
                _removedOrbs.Add(orb);
                continue;
            }

            if (orb.State is Orb.FollowState followState)
            {
                followState.Target = data.TargetFunc.Invoke();
            }

            _orbDataBuff.Add(data);
        }

        (_orbData, _orbDataBuff) = (_orbDataBuff, _orbData);
        _orbDataBuff.Clear();
        _removedOrbs.Clear();

        if (toClear)
        {
            _orbData.Clear();
            toClear = false;
        }
    }

    public void LateUpdate()
    {
        
    }
}