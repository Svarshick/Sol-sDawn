using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic.Configs;

public class DebugStats
{
    public Color HitColliderColor;
    [Units] public float HitColliderWidth;
    public Color ParryColliderColor;
    [Units] public float ParryColliderWidth;
}

//length in units
//time in seconds
public static partial class MainConfig
{
    
    public static readonly PlayerStats PlayerStats = new()
    {
        Color = Color.Blue,
        Radius = 0.5f,
        Velocity = 5,
        
        BladeAttackDistance = 1,
        BladeAttackAngle = 40,
        BladeAttackLength = 2,
        BladeAttackWidth = 0.5f,
        BladeDashDistance = 2,
        BladeDashWidth = 1f,
        BladeTraceDuration = 1f,
        BladeAimDistance = 3f,
        BladeAimRadius = 0.2f,
        BladeAimColor = new Color(0, 255, 255, 122),
        
        FireDistance = 10f,
        FireWidth = 0.3f,
        FireTraceDuration = 1,
        FireTraceWidth = 0.3f,
        FireTraceColor = Color.Red,
        
        TeleportMinDistance = 3,
        TeleportMaxDistance = 7,
        TeleportHoldDuration = 0.5f,
        TeleportWidth = 0.3f,
        TeleportStartColor = Color.Transparent,
        TeleportEndColor = Color.Aqua,
        TeleportTraceWidth = 1,
        TeleportTraceStartColor = Color.Aquamarine,
        TeleportTraceEndColor = Color.Transparent
    };

    public static readonly BossStats BossStats = new()
    {
        Color = Color.BlueViolet,
        Radius = 0.5f,

        BladeTelegraphDuration = 0.5f,

        BladeAttackDistance = 1.5f,
        BladeAttackAngle = 45f,
        BladeAttackLength = 2.5f,
        BladeAttackWidth = 0.7f,
        BladeDashDistance = 2.5f,
        BladeDashWidth = 1.2f,
        BladeTraceDuration = 1.2f,
        BladeAimDistance = 4f,
        BladeAimRadius = 0.3f,
        BladeAimColor = new Color(255, 0, 255, 122),

        ParryDuration = 1f,

        TeleportTraceWidth = 1f,
        TeleportTraceStartColor = Color.Red,
        TeleportTraceEndColor = Color.Transparent
    };

    public static readonly DebugStats DebugStats = new()
    {
        HitColliderColor = Color.GreenYellow,
        HitColliderWidth = 0.1f,
        ParryColliderColor = Color.Orange,
        ParryColliderWidth = 0.1f,
    };
}