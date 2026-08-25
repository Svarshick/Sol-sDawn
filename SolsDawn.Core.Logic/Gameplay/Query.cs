using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

namespace SolsDawn.Core.Logic.Gameplay;

public static class Query
{
    /*public static void Overlap(
        Shape shape,
        Vector2 position,
        float rotation,
        Category layers, 
        IList<GameObject> results,
        DebugCategory debugCategory)
    {
        Collision.Overlap(
            shape,
            position,
            rotation,
            layers,
            results);

        Color color;
        float time;
        bool enabled;
        switch (debugCategory)
        {
            case DebugCategory.Default:
                enabled = Debug.ColliderEnabled;
                color = Debug.ColliderColor;
                time = Debug.ColliderMinimalTime;
                break;
            case DebugCategory.Parry:
                enabled = Debug.ParryEnabled;
                color = Debug.ParryColor;
                time = Debug.ParryMinimalTime;
                break;
            case DebugCategory.Attack:
                enabled = Debug.AttackEnabled;
                color = Debug.AttackColor;
                time = Debug.AttackMinimalTime;
                break;
            default:
                return;
        }

        if (!enabled)
            return;

        //void Drawer() => SolsDawn.SpriteBatch.DrawShape(shape, position, rotation, color);
        //SolsDawn.AnimationsPool.Add( new DelegatedAnimation(Drawer, time));
    }*/
}