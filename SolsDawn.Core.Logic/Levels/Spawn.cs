using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Entities;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Levels;

public static class Spawn
{
    public static Platform Platform(Vector2 position, float width, float rotation = 0) =>
        new(CreateObject(position, rotation), width);

    public static Player Player(Vector2 position, CategorySlot<PlayerSpecs> specs)
    {
        if (G.Player is { IsDestroyed: false })
            Log.Logic.Fatal($"Level {LevelRunner.CurrentName} spawned a second player.");

        var board = new PlayerBoard { Specs = specs };
        var player = new Player(CreateObject(position), board, new PlayerAnimations(board));
        G.Player = player;
        return player;
    }

    public static Enemy Enemy(Vector2 position, CategorySlot<EnemySpecs> specs, Func<Enemy, Job> behaviour)
    {
        var board = new EnemyBoard { Specs = specs };
        var enemy = new Enemy(CreateObject(position), board, new EnemyAnimations(board));
        enemy.Behaviour = behaviour(enemy);
        return enemy;
    }
}
