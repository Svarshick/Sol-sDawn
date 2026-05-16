using Microsoft.Xna.Framework;
using SolsDawn.Core.Gameplay;

namespace SolsDawn.Core.Configs;

public static class MainConfig
{
    //length in units
    //time in seconds
    public static readonly PlayerStats PlayerStats = new(
        Velocity: 5,
        BladeDistance: 3,
        TeleportMinDistance: 3,
        TeleportMaxDistance: 7,
        TeleportHoldDuration: 0.5f,
        TeleportThickness: 0.3f,
        TeleportStartColor: Color.Transparent,
        TeleportEndColor: Color.Aqua,
        TeleportTraceThickness: 1,
        TeleportTraceStartColor: Color.Aquamarine,
        TeleportTraceEndColor: Color.Transparent
    );
}