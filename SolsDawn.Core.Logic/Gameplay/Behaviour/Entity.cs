using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class Entity : Component<Entity>
{
    public readonly HP HP;
    public Transform2 Transform => GameObject.Transform;
    
    public Entity(GameObject go) : base(go)
    {
        HP = go.GetComponent<HP>() ?? throw new ComponentNotFoundException<HP>();
    }
    
    public override void Dispose()
    {
    }

    public void Kill()
    {
        GameObject.Dispose();
    }
}