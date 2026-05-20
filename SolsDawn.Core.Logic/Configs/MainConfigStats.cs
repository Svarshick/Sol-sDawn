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
        Width = 0.7f,
        Height = 1.3f,
        Velocity = 5,
        
        TeleportRechargeDuration = 1.5f,
        BladeRechargeDuration = 1f,
        FireRechargeDuration = 2f,
    
        HitInvulnerabilityDuration = 0.5f,
        HitBlinkColor = Color.Red,
        
        BladeAttackDistance = 1,
        BladeAttackEdgeAngle = 40,
        BladeAttackEdgeLength = 2,
        BladeAttackEdgeWidth = 0.5f,
        BladeDashDistance = 2,
        BladeDashWidth = 1f,
        BladeTraceDuration = 1f,
        BladeTraceStartColor = Color.White,
        BladeTraceEndColor = Color.Transparent,
        BladeAimDistance = 3f,
        BladeAimRadius = 0.2f,
        BladeAimColor = new Color(0, 255, 255, 122),
        
        FireDistance = 10f,
        FireWidth = 0.3f,
        FireTraceDuration = 1,
        FireTraceWidth = 0.3f,
        FireTraceStartColor = Color.Red,
        FireTraceEndColor = Color.Transparent,
        
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
        Width = 0.7f,
        Height = 1.4f,
        
        HitDuration = 0.5f,
        HitBlinkColor = Color.Red,
        
        BladeTelegraphDuration = 0.5f,
        BladeTelegraphBlinkColor = Color.White,

        BladeAttackDistance = 1.5f,
        BladeAttackEdgeAngle = 45f,
        BladeAttackEdgeLength = 2.5f,
        BladeAttackEdgeWidth = 0.7f,
        BladeDashDistance = 2.5f,
        BladeDashWidth = 1.2f,
        BladeTraceDuration = 1.2f,
        BladeTraceStartColor = Color.White,
        BladeTraceEndColor = Color.Transparent,

        ParryDuration = 1f,
        ParryColor = Color.White,

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