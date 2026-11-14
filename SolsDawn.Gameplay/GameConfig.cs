using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay;

public class GameConfig
{
    [PlayerCategory]
    public CategorySlot<PlayerSpecs> Player = new();

    [EnemyCategory]
    public CategorySlot<EnemySpecs> Enemy = new();
}
