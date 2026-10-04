using SolsDawn.Gameplay.Entities;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay;

public static class GameLoop
{
    public static async Job Start()
    {
        ImGuiDrawer = Debug.Draw;
        BeforeUpdate();
        Update();
        AfterUpdate();

        while (true)
        {
            await NextFrame();
        }
    }
    
    private static async Job BeforeUpdate()
    {
        var floor = new Platform(CreateObject(), 100);
        floor.Transform.Position = new Vector2(0, 0);
        var leftSlope = new Platform(CreateObject(), 10);
        leftSlope.Transform.Position = new Vector2(-5, 0);
        leftSlope.Transform.Rotation = -PI * 1 / 4;
        var negSlope = new Platform(CreateObject(), 10);
        negSlope.Transform.Position = new Vector2(-7, 3);
        negSlope.Transform.Rotation = -PI * 5 / 8;
        var highPlatform = new Platform(CreateObject(), 10);
        highPlatform.Transform.Position = new Vector2(0, 4);
        var rightSlope = new Platform(CreateObject(), 100);
        rightSlope.Transform.Position = new Vector2(60, 10);
        rightSlope.Transform.Rotation = ASin(10 / 50f);
        
        var playerBoard = new PlayerBoard();
        var playerAnimations = new PlayerAnimations(playerBoard);
        G.Player = new Player(
            CreateObject(),
            playerBoard,
            playerAnimations);
        G.Player.Transform.Position = new Vector2(0, 1);
        var playerController = new PlayerController(G.Player);
        
        while (true)
        {
            playerController.Update();
            await NextFrame();
        }
    }

    private static async Job Update()
    {
        while (true)
        {
            await NextFrame();
        }
    }

    private static async Job AfterUpdate()
    {
        while (true)
        {
            Camera.Position = G.Player.Transform.Position;
            await NextFrame();
        }
    }
}

public static class G
{
    public static Player Player;
}