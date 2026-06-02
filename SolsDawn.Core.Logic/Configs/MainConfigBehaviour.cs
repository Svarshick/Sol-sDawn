using System.Runtime.CompilerServices;
using MonoGame.Extended.VectorDraw;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic.Configs;
using static BossBehaviourBuilder;

public static partial class MainConfig
{
    public static BossBehaviourBuilder BossBehaviourBuilder => FireSuffer;

    private static BossBehaviourBuilder BladeTest => Create()
        .Teleport(Player + Units(0, -1))
        .Wait(1)
        .Blade(Player);

    private static BossBehaviourBuilder OrbTest => Create()
        .Wait(0.5f)
        .SpawnOrb(Player + Units(0, -3), Player, DefaultOrbStats)
        .Teleport(Player + Units(0, -4))
        .Fire(Player);
    
    private static BossBehaviourBuilder TestAll => Create()
        .Wait(0.5f)
        .Teleport(Units(3, 3))
        .Wait(1)
        .Blade(Units(0, 0))
        .Wait(0.5f)
        .Teleport(Units(3, -3))
        .Wait(1)
        .Fire(Units(0, 0))
        .Wait(0.5f)
        .SpawnOrb(Player + Units(0, -1), Player, DefaultOrbStats);

    private static BossBehaviourBuilder IfTest => Create()
        .Wait(0.5f)
        .If((Player - Boss).Magnitude() > Units(10))
            .Teleport(Player + Units(0, -2))
        .Else()
            .Fire(Player)
        .EndIf();

    private static BossBehaviourBuilder WhileTest => Create()
        .Wait(1f)
        .While(!IsBossLastBladeParried)
            .Blade(Player)
            .Wait(1f)
            .Fire(Player)
            .If(IsBossLastFireParried)
                .Wait(0.5f)
                .Fire(Player)
            .EndIf()
            .Wait(1f)
        .EndWhile()
        .Forget()
        .SpawnOrb(Boss, Player, DefaultOrbStats);

    private static BossBehaviourBuilder CountTest => Create()
        .While(Count(3))
        .Wait(1f)
        .Fire(Player)
        .EndWhile()
        .SpawnOrb(Player + Units(0, -2), PlayerSnapshot, DefaultOrbStats);

    private static BossBehaviourBuilder FireAndOrb => Create()
        .Wait(1f)
        .SpawnOrb(Boss + Rotate(Normalize(Boss - Player) * Var.OrbDistance, Var.OrbAngle), Player, DefaultOrbStats)
        .SpawnOrb(Boss + Normalize(Boss - Player) * Var.OrbDistance, Player, DefaultOrbStats)
        .SpawnOrb(Boss + Rotate(Normalize(Boss - Player) * Var.OrbDistance, -Var.OrbAngle), Player, DefaultOrbStats)
        
        //углы передаются в радианах.
        //Если пользуешься Var переменной с [Euler], то оно автоматически переведет в радианы
        //Если не хочешь писать в Var, то придется писать float.DegreesToRadians(угол)
        //Также ты можешь писать в Var без [Euler], и тогда придется float.DegreesToRadians(Var.Угол). Зачем? Вот именно, незачем
        .SpawnOrb(
            Boss + Rotate(Normalize(Boss - Player) * Var.OrbDistance, float.DegreesToRadians(30)),
            Player,
                DefaultOrbStats)
        .Fire(Player);

    private static BossBehaviourBuilder FireAndOrbCrook => Create()
        .While(Count(100))
        .Then(() => FireAndOrb)
        .EndWhile();
    
    
    //======== ALNORA ==========


    private static readonly BossBehaviourBuilder Alnora = Create()
        .Then(() => LetsTeachYouBladeParry)
        .Then(() => LetsTeachYouFireParry)
        .Then(() => CrestOfMechan)
        //вау ты выжил, ну подыши хз
        .Wait(0.5f);


    private static BossBehaviourBuilder PulseXTwelve => Create()
    //прям по часовой
    .SpawnOrb(Boss, Normalize(Boss + Units(0, -1))*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*2)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*3)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*4)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*5)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*6)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*7)*Var.OrbDistance*100,DefaultOrbStats)
  
    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*8)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*9)*Var.OrbDistance*100,DefaultOrbStats)

    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*10)*Var.OrbDistance*100,DefaultOrbStats)
    
    .SpawnOrb(Boss, Rotate(Normalize(Boss + Units(0, -1)), Var.OrbAngle*11)*Var.OrbDistance*100,DefaultOrbStats)
    .Wait(0f);

    private static BossBehaviourBuilder RadiumRecoil => Create()
    //5 орбов отдачи
    .SpawnOrb(Boss, Normalize(Boss - Player)*Var.OrbDistance*100, AlnoraRecoilOrbsStats)
    .SpawnOrb(Boss, Rotate(Normalize(Boss - Player)*Var.OrbDistance*100, Var.OrbAngle), AlnoraRecoilOrbsStats)
    .SpawnOrb(Boss, Rotate(Normalize(Boss - Player)*Var.OrbDistance*100, -Var.OrbAngle), AlnoraRecoilOrbsStats)
    .SpawnOrb(Boss, Rotate(Normalize(Boss - Player)*Var.OrbDistance*100, Var.OrbAngle*2), AlnoraRecoilOrbsStats)
    .SpawnOrb(Boss, Rotate(Normalize(Boss - Player)*Var.OrbDistance*100, -Var.OrbAngle*2), AlnoraRecoilOrbsStats);


    private static BossBehaviourBuilder AVSnipeShot => Create()
    .Fire(Player)
    .Then(() => RadiumRecoil)
    .Wait(0.5f);

    private static BossBehaviourBuilder LetsTeachYouBladeParry => Create()

        .Teleport(Player + Units(2, 0))
        .Blade(Player)

        .Wait(1)

        .Teleport(Player + Units(2, 2))
        .Blade(Player)

        .Wait(1)

        .Teleport(Player + Units(2, -2))
        .Blade(Player)

        .Wait(2f);

    private static BossBehaviourBuilder FireSuffer => Create()
        .Teleport(Player + (CameraBottomLeft - Player)*(3f/4))
        .Then(()=> PulseXTwelve)
        .Wait(1f)
        .Fire(Player)

        .Teleport(Player + (CameraBottomRight - Player)*(3f/4))
        .Then(()=> PulseXTwelve)
        .Wait(1f)
        .Fire(Player)

        .Teleport(Player + (CameraTopRight - Player)*(3f/4))
        .Then(()=> PulseXTwelve)
        .Wait(1f)
        .Fire(Player)

        .Teleport(Player + (CameraTopLeft - Player)*(3f/4))
        .Then(()=> PulseXTwelve)
        .Wait(1f)
        .Fire(Player)

        .Wait(0.5f);



    private static BossBehaviourBuilder LetsTeachYouFireParry => Create()
        .Teleport(CameraBottomLeft)
        .Wait(1f)
        .Fire(Player)

        .Teleport(CameraBottomRight)
        .Wait(1f)
        .Fire(Player)

        .Teleport(CameraTopRight)
        .Wait(1f)
        .Fire(Player)

        .Teleport(CameraTopLeft)
        .Wait(1f)
        .Fire(Player)

        .Wait(0.5f);

    private static BossBehaviourBuilder CrestOfMechan => Create()
        //Ульта!
        //посмотрим, на сколько тебя хватит :)
        .Teleport(Player + Units(2, -3))
        .Wait(0.25f)
        .Blade(Player)
        .Teleport(Player + Units(-2, 3))
        .Wait(0.25f)
        .Fire(Player)
        //И опять!
        .Teleport(Player + Units(-2, -3))
        .Wait(0.25f)
        .Blade(Player)
        .Teleport(Player + Units(2, 3))
        .Wait(0.25f)
        .Fire(Player)
        //AND ONCE AGAIN
        .Teleport(Player + Units(4, 0))
        .Wait(0.25f)
        .Blade(Player)
        .Teleport(Player + Units(-4, 0))
        .Wait(0.25f)
        .Fire(Player)

        //AHAHAHHAHAHAHAAHHA
        .Wait(1.5f);
}