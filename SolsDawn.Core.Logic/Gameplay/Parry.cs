namespace SolsDawn.Core.Logic.Gameplay;

public enum ParryType
{
    None,
    Blade,
    Fire,
}

public class Parry(
    GameObject go, 
    GameObject target,
    ParryType type) 
    : Component<Parry>(go)
{

    public readonly ParryType Type = type;
    public readonly GameObject Target = target;

    public override void Dispose()
    {
    }
}