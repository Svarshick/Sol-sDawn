using System;
using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static async Job RootJob()
    {
        BeforeGameLoop();
        GameLoop();
        AfterGameLoop();

        while (true)
        {
            await NextFrame();
        }
    }

    private static async Job BeforeGameLoop()
    {
        var playerObj = CreateObject();
        var playerBoard = new PlayerBoard();
        G.Player = new Player(playerObj, playerBoard, new PlayerAnimations(playerBoard));
        var playerController = new PlayerController(G.Player);
        
        var hudObj = CreateObject();
        new HUD(hudObj, G.Player);
        
        while (true)
        {
            playerController.Update();
            await NextFrame();
        }
    }
    
    private static async Job GameLoop()
    {
        var bossObj = CreateObject();
        var bossBoard = new BossBoard();
        var boss = new Boss(bossObj, bossBoard, new BossAnimations(bossBoard));

        Actions.Tests.OrbSpam();
        while (true)
        {
            await Actions.SimpleActions.FireAttack(boss);
            await Timer(2);
        }
    }

    private static async Job AfterGameLoop()
    {
        while (true)
        {
            Camera.Position = G.Player.GameObject.Transform.Position;
            await NextFrame();
        }
    }
}

public static class G
{
    public static Player Player;
}