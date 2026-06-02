using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Configs;
using static BossBehaviourBuilder;

public static partial class MainConfig
{
    public static BossBehaviourBuilder BossBehaviourBuilder => OrbTest;

    private static BossBehaviourBuilder BladeTest => Create()
        .Teleport(Player + Units(0, -1))
        .Wait(1)
        .Blade(Player);

    private static BossBehaviourBuilder OrbTest => Create()
        .Wait(1f)
        .SpawnOrb(Player + Units(0, -4), Player, DefaultOrbStats);
    
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

    private static readonly BossBehaviourBuilder AlnoraV = Create()
        .Then(() => LetsTeachYouBladeParry)
        .Then(() => LetsTeachYouFireParry)
        .Then(() => CrestOfMechan)
        //вау ты выжил, ну подыши хз
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