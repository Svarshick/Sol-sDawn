using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Gameplay.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class EntityStats
{
    public Color Color;
    public float Width;
    public float Height;
}

public class Entity : Component
{
    public readonly EntityStats Stats;
    
    //public readonly HP HP;
    public readonly Animator<EntityAnimations> Animator;
    public Transform2 Transform => GameObject.Transform;
    
    public Entity(GameObject go, EntityStats stats) : base(go, true)
    {
        //HP = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
        Animator = go.GetComponent<Animator<EntityAnimations>>() ?? throw new ComponentNotFoundException<Animator<EntityAnimations>>();
        Stats = stats;
    }

    public void Kill() => Destroy();
}