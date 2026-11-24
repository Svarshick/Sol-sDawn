namespace SolsDawn.Gameplay;

public class GameConfig
{
    [PlayerCategory]
    public CategorySlot<PlayerSpecs> Player = new();

    [EnemyCategory]
    public CategorySlot<EnemySpecs> Enemy = new();

    [CameraCategory]
    public CategorySlot<CameraSpecs> Camera = new();
}
