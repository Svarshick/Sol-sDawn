namespace SolsDawn.Core.Logic.Gameplay;

public class Parry(
    GameObject go, 
    GameObject parryTarget) 
    : Component<Parry>(go)
{
    public readonly GameObject Target = parryTarget;

    public override void Dispose()
    {
    }
}