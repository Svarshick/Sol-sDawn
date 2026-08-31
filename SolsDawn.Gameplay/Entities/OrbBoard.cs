namespace SolsDawn.Gameplay.Entities;

public class OrbBoard
{
    public OrbSpecs Specs = new();
}

public record OrbSpecs
{
    public Color Color = Color.LightSalmon;
    public float Radius = 0.3f;
    public float Velocity = 3;
}