using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Configs;
using static BossBehaviourBuilder;

public static partial class MainConfig
{
    public static BossBehaviourBuilder BossBehaviourBuilder => B3;
    
    private static BossBehaviourBuilder B1 => Create()
        .Teleport(CameraBottomLeft)
        .Teleport(CameraBottomRight)
        .Wait(1)
        .Teleport(Player + Units(0, -1));
    
    private static BossBehaviourBuilder B2 => Create()
        .Teleport(Units(3, 3))
        .Wait(1)
        .Teleport(Units(3, -3))
        .Teleport(Units(-3, -3))
        .Wait(2.5f)
        .Blade(Units(-3, 3))
        .Wait(1);

    private static BossBehaviourBuilder B3 => Create()
        .Wait(2)
        .Teleport(Units(3, 3))
        .Blade(Units(0, 0));

    private static readonly BossBehaviourBuilder B4 = Create()
        .Teleport(Units(0, 0))
        .Then(() => B1)
        .Wait(2.3f);
}