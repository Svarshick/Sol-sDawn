using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class Entity : Component
{
    public readonly HP HP;
    public Transform2 Transform => GameObject.Transform;
    
    public Entity(GameObject go) : base(go, true)
    {
        HP = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
    }

    public void Kill() => Destroy();
}