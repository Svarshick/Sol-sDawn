using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Core.Logic.Configs;

public class DebugStats
{
    public Color HitColliderColor = Color.GreenYellow;
    [Units] public float HitColliderWidth = 0.1f;
    public Color ParryColliderColor = Color.Orange;
    [Units] public float ParryColliderWidth = 0.1f;
}

//length in units
//time in seconds
public class Variables
{
    public float TelegraphDuration = 0.2f;
}

//length in units
//time in seconds
public static partial class MainConfig
{
    public static PlayerStats PlayerStats => PlayerStatsLazy.Value;
    public static BossStats BossStats => BossStatsLazy.Value;
    public static DebugStats DebugStats => DebugStatsLazy.Value;

    private static readonly Lazy<PlayerStats> PlayerStatsLazy
        = new(() => ConfigReader.Read(DesignPlayerStats, Game.ScreenLayout));

    private static readonly Lazy<BossStats> BossStatsLazy
        = new(() => ConfigReader.Read(DesignBossStats, Game.ScreenLayout));

    private static readonly Lazy<DebugStats> DebugStatsLazy
        = new(() => ConfigReader.Read(DesignDebugStats, Game.ScreenLayout));

    private static Variables Var => VariablesLazy.Value;
    private static readonly Lazy<Variables> VariablesLazy = new();

    private static readonly PlayerStats DesignPlayerStats = new()
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
        BladeTraceDuration = 0.4f,
        BladeTraceStartColor = Color.Aquamarine,
        BladeTraceEndColor = Color.Transparent,
        BladeAimDistance = 3f,
        BladeAimRadius = 0.2f,
        BladeAimColor = new Color(0, 255, 255, 122),

        BladeParryTraceDuration = 0.7f,
        BladeParryTraceStartColor = Color.White,
        BladeParryTraceEndColor = Color.Transparent,

        FireDistance = 12f,
        FireWidth = 0.3f,
        FireTraceDuration = 1f,
        FireTraceWidth = 0.3f,
        FireTraceStartColor = Color.Coral,
        FireTraceEndColor = Color.Transparent,

        FireParryTraceDuration = 1.5f,
        FireParryTraceStartColor = Color.Gold,
        FireParryTraceEndColor = Color.Transparent,


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

    private static readonly BossStats DesignBossStats = new()
    {
        Color = Color.BlueViolet,
        Width = 0.7f,
        Height = 1.4f,

        HitDuration = 0.5f,
        HitBlinkColor = Color.Red,

        BladeTelegraphDuration = Var.TelegraphDuration,
        BladeTelegraphBlinkColor = Color.White,

        BladeAttackDistance = 1.5f,
        BladeAttackEdgeAngle = 45f,
        BladeAttackEdgeLength = 2.5f,
        BladeAttackEdgeWidth = 0.7f,
        BladeDashDistance = 2.5f,
        BladeDashWidth = 1.2f,
        BladeTraceDuration = 0.4f,
        BladeTraceStartColor = Color.DeepPink,
        BladeTraceEndColor = Color.Transparent,

        BladeParriedDuration = 1.5f,
        BladeParriedColor = Color.White,
        BladeParryTraceDuration = 0.7f,
        BladeParryTraceStartColor = Color.White,
        BladeParryTraceEndColor = Color.Transparent,

        FireTelegraphDuration = Var.TelegraphDuration,
        FireTelegraphBlinkColor = Color.MediumSpringGreen,

        FireDistance = 15,
        FireWidth = 0.3f,
        FireTraceDuration = 1f,
        FireTraceStartColor = Color.MediumTurquoise,
        FireTraceEndColor = Color.Transparent,

        FireParriedDuration = 1f,
        FireParriedColor = Color.MediumSpringGreen,
        FireParryTraceDuration = 1.5f,
        FireParryTraceStartColor = Color.MediumSpringGreen,
        FireParryTraceEndColor = Color.Transparent,

        TeleportTraceWidth = 1f,
        TeleportTraceStartColor = Color.DeepSkyBlue,
        TeleportTraceEndColor = Color.Transparent
    };

    private static readonly DebugStats DesignDebugStats = new();
}