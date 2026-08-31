using System;
using System.Collections.Generic;

namespace SolsDawn.Core.Logic.Gameplay;

public abstract record CollisionRecord;

public record HitCollision(HitContext Context) : CollisionRecord;
public record BladeParryCollision(BladeParryContext Context) : CollisionRecord;

public class CollisionsPool
{
    private readonly List<CollisionRecord> _collisions = new();

    public void Add(CollisionRecord record)
    {
        if (!Collision.World.IsLocked)
            throw new InvalidOperationException("Can't add collision record outside World update");
        
        _collisions.Add(record);
    }

    public void Resolve()
    {
        foreach (var collision in _collisions)
        {
            switch (collision)
            {
                case HitCollision hit:
                {
                    var context = hit.Context;
                    context.Attack.ExecuteHit(context);
                    break;
                }
                case BladeParryCollision bladeParry:
                {
                    var context = bladeParry.Context;
                    context.Attack.ExecuteParry(context);
                    context.ParryWindow.Execute(context);
                    break;
                }
                default:
                {
                    throw new LogicException();
                }
            }
        }
        
        _collisions.Clear();
    } 
}