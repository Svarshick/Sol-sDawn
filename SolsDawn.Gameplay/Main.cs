using SolsDawn.Gameplay.Entities;
using SolsDawn.Gameplay.Utils;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static async Job RootJob()
    {
        ImGuiDrawer = Debug.Draw;
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
        G.Floor = new Platform(CreateObject(), 100);
        G.Floor.Transform.Position = new Vector2(0, -1);
        
        var slope = new Platform(CreateObject(), 10);
        slope.Transform.Position = new Vector2(-5, 0);
        slope.Transform.Rotation = -PI * 1 / 4;
        var negSlope = new Platform(CreateObject(), 10);
        negSlope.Transform.Position = new Vector2(-7, 3);
        negSlope.Transform.Rotation = -PI * 5 / 8;
        var highPlatform = new Platform(CreateObject(), 10);
        highPlatform.Transform.Position = new Vector2(0, 4);
        
        var playerBoard = new PlayerBoard();
        var playerAnimations = new PlayerAnimations(playerBoard);
        G.Player = new Player(
            CreateObject(),
            playerBoard,
            playerAnimations);
        
        var playerController = new PlayerController(G.Player);
        
        while (true)
        {
            playerController.Update();
            await NextFrame();
        }
    }
    
    private static async Job GameLoop()
    {
        while (true)
        {
            await NextFrame();
        }
    }

    private static async Job AfterGameLoop()
    {
        while (true)
        {
            var playerPosition = G.Player.GameObject.Transform.Position;
            Camera.Position = playerPosition;
            await NextFrame();
        }
    }
}

public static class G
{
    public static Platform Floor;
    public static Player Player;
}