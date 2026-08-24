using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay;

public static class Main
{
    public static Player Player;
    public static PlayerController PlayerController;
    
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
        new Animator<DefaultAnimation>(playerObj, new DefaultAnimation());
        Player = new Player(playerObj, PlayerBoard);
        PlayerController = new PlayerController(Player);
        
        var hudObj = CreateObject();
        new HUD(hudObj, Player);

        while (true)
        {
            PlayerController.Update();
            IntentionsPool.Resolve();
            await NextFrame();
        }
    }
    
    private static async Job GameLoop()
    {
        var bossObj = CreateObject();
        new Animator<DefaultAnimation>(bossObj, new DefaultAnimation());
        var boss = new Boss(bossObj, BossBoard);
        
        while (true)
        {
            await Actions.SimpleAttack(boss);
            await Timer(2);
        }
    }

    private static async Job AfterGameLoop()
    {
        while (true)
        {
            Camera.Position = Player.GameObject.Transform.Position;
            await NextFrame();
        }
    }

    public static PlayerBoard PlayerBoard = new()
    {
        Config =
        {
            Color = Color.Blue,
            Width = 0.7f,
            Height = 1.3f,
            Velocity = 6,
            CursorRadius = 0.2f,
            CursorColor = new Color(0, 255, 255, 122),

            TeleportRechargeDuration = 1.5f,
            BladeRechargeDuration = 1f,
            FireRechargeDuration = 2f,

            HitInvulnerabilityDuration = 0.5f,
            HitBlinkColor = Color.Red,

            BladeAttackDistance = 1,
            BladeAttackWidth = 2f,
            BladeDashDistance = 2,
            BladeDashWidth = 1f,
            BladeTraceDuration = 0.4f,
            BladeTraceStartColor = Color.Aquamarine,

            BladeParryTraceDuration = 0.7f,
            BladeParryTraceStartColor = Color.White,
            BladeParryTraceEndColor = Color.Transparent,
            BladeParryPushDistance = 2f,
            BladeParryPushVelocity = 15f,

            FireDistance = 20f,
            FireWidth = 0.4f,
            FireTraceDuration = 0.7f,
            FireTraceWidth = 0.3f,
            FireTraceStartColor = Color.Coral,
            FireTraceEndColor = Color.Transparent,

            FireParryTraceDuration = 0.7f,
            FireParryTraceStartColor = Color.Gold,
            FireParryTraceEndColor = Color.Transparent,


            TeleportMinDistance = 4,
            TeleportMaxDistance = 9,
            TeleportHoldDuration = 0.6f,
            TeleportWidth = 0.3f,
            TeleportStartColor = Color.Transparent,
            TeleportEndColor = Color.Aqua,
            TeleportTraceWidth = 1,
            TeleportTraceDuration = 1,
            TeleportTraceStartColor = Color.Aquamarine,
            TeleportTraceEndColor = Color.Transparent
        }
    };

    private static float _bossStatsTelegraphDuration = 0.4f;

    public static BossBoard BossBoard = new()
    {
        Color = Color.BlueViolet,
        Width = 0.7f,
        Height = 1.4f,

        HitDuration = 0.5f,
        HitBlinkColor = Color.Red,

        BladeTelegraphDuration = _bossStatsTelegraphDuration,
        BladeTelegraphBlinkColor = Color.White,
        BladeTelegraphStarDuration = _bossStatsTelegraphDuration / 2,
        BladeTelegraphStarDistance = 2.5f,
        BladeTelegraphStarColor = Color.Yellow,
        BladeTelegraphStarInnerRadius = 0.2f,
        BladeTelegraphStarOuterRadius = 2,
        BladeTelegraphStarStartAngle = 0,
        BladeTelegraphStarDeltaAngle = 90,
        BladeTelegraphStarThickness = 0.1f,

        BladeAttackDistance = 1.5f,
        BladeAttackEdgeAngle = 45f,
        BladeAttackEdgeLength = 2.5f,
        BladeAttackEdgeWidth = 0.7f,
        BladeDashDistance = 2.5f,
        BladeDashWidth = 1.2f,
        BladeTraceDuration = 0.4f,
        BladeTraceStartColor = Color.DeepPink,
        BladeTraceEndColor = Color.Transparent,

        BladeParriedDuration = 0.3f,
        BladeParriedColor = Color.White,
        BladeParriedPushDistance = 2f,
        BladeParriedPushVelocity = 15f,
        BladeParryTraceDuration = 0.7f,
        BladeParryTraceStartColor = Color.White,
        BladeParryTraceEndColor = Color.Transparent,

        FireTelegraphDuration = _bossStatsTelegraphDuration,
        FireTelegraphAimingDuration = _bossStatsTelegraphDuration / 2,
        FireTelegraphBlinkColor = Color.MediumSpringGreen,

        FireDistance = 15,
        FireWidth = 0.3f,
        FireTraceDuration = 1f,
        FireTraceStartColor = Color.MediumTurquoise,
        FireTraceEndColor = Color.Transparent,

        FireParriedDuration = 1f,
        FireParriedColor = Color.MediumSpringGreen,
        FireParryTraceDuration = 1.5f,
        FireParryTraceStartColor = Color.Red,
        FireParryTraceEndColor = Color.Transparent,

        TeleportTraceWidth = 1f,
        TeleportTraceDuration = 1,
        TeleportTraceStartColor = Color.DeepSkyBlue,
        TeleportTraceEndColor = Color.Transparent
    };
}