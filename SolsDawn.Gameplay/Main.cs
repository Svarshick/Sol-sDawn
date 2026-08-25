using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static async Job RootJob()
    {
        IntentionsPool.ResolveLogic = Intentions.ResolveLogic;
        
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
        G.Player = new Player(playerObj, new PlayerBoard(), new DefaultAnimation());
        var playerController = new PlayerController(G.Player);
        
        var hudObj = CreateObject();
        new HUD(hudObj, G.Player);

        while (true)
        {
            playerController.Update();
            IntentionsPool.Resolve();
            await NextFrame();
        }
    }
    
    private static async Job GameLoop()
    {
        var bossObj = CreateObject();
        var boss = new Boss(bossObj, new BossBoard(), new DefaultAnimation());
        
        while (true)
        {
            await Actions.SimpleActions.Attack(boss);
            //await Actions.Tests.Collider();
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