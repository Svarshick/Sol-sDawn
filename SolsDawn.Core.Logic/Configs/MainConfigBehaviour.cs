using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Configs;
using static BossBehaviourBuilder;

public static partial class MainConfig
{
    public static readonly BossBehaviourBuilder BossBehaviourBuilder = new BossBehaviourBuilder()
        .Wait(1)
        .Teleport(Units(2, 2))
        .Wait(1)
        .Teleport(Units(-2, -2))
        .Blade(Player)
        .Wait(1)
        .Teleport(Units(-2, 2));
}
