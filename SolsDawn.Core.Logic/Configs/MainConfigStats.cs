using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Configs;

public class DebugStats
{
    public Color HitColliderColor = Color.GreenYellow;
    public float HitColliderWidth = 0.1f;
    public Color ParryColliderColor = Color.Orange;
    public float ParryColliderWidth = 0.1f;
}

//length in units
//time in seconds
public class Variables
{
    public float TelegraphDuration = 0.4f; //времени [Аттрибут] не нужен. По умолчанию время в секундах 
    public float OrbDistance = 2f; //[Units] чтобы конфиг писать в юнитах и игра сама переведет юниты в пиксели
    [Euler] public float OrbAngle = 30; //[Euler] чтобы углы писать в эйлере и игра сама переведет ейлера в радианы
}

//length in units
//time in seconds
public static partial class MainConfig
{
    public static PlayerStats PlayerStats => PlayerStatsLazy.Value;
    public static BossStats BossStats => BossStatsLazy.Value;
    public static OrbStats DefaultOrbStats => OrbStatsLazy.Value;

    public static OrbStats AlnoraRecoilOrbsStats => OrbStatsRecoilLazy.Value;
    public static Variables Var => VariablesLazy.Value;
    public static DebugStats DebugStats => DebugStatsLazy.Value;

    private static readonly Lazy<PlayerStats> PlayerStatsLazy
        = new(() => ConfigReader.Read(DesignPlayerStats));

    private static readonly Lazy<BossStats> BossStatsLazy
        = new(() => ConfigReader.Read(DesignBossStats));

    private static readonly Lazy<OrbStats> OrbStatsLazy
        = new(() => ConfigReader.Read(DesignDefaultOrbStats));

    private static readonly Lazy<OrbStats> OrbStatsRecoilLazy
        = new(() => ConfigReader.Read(DesignRecoilOrbStats));

    private static readonly Lazy<DebugStats> DebugStatsLazy
        = new(() => ConfigReader.Read(DesignDebugStats));

    private static readonly Lazy<Variables> VariablesLazy = 
        new(() => ConfigReader.Read(new Variables()));

    private static readonly PlayerStats DesignPlayerStats = new()
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
        BladeAttackEdgeAngle = 40,
        BladeAttackEdgeLength = 2,
        BladeAttackEdgeWidth = 0.5f,
        BladeDashDistance = 2,
        BladeDashWidth = 1f,
        BladeTraceDuration = 0.4f,
        BladeTraceStartColor = Color.Aquamarine,
        BladeTraceEndColor = Color.Transparent,
        
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
        BladeTelegraphStarDuration = Var.TelegraphDuration/2,
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

        FireTelegraphDuration = Var.TelegraphDuration,
        FireTelegraphAimingDuration = Var.TelegraphDuration * 1/2,
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

    private static readonly OrbStats DesignDefaultOrbStats = new()
    {
        Color = Color.Yellow,
        Radius = 0.2f,
        Velocity = 4,

        ExplosionRadius = 1.5f,
        ExplosionColor = Color.Orange,
        ExplosionTraceDuration = 0.5f,
    };
    
   private static readonly OrbStats DesignRecoilOrbStats = new()
    {
        Color = Color.BlueViolet,
        Radius = 0.2f, 
        Velocity = 6,

        ExplosionRadius = 1,
        ExplosionColor = Color.Aquamarine, 
        ExplosionTraceDuration = 0.5f,
    };

    private static readonly DebugStats DesignDebugStats = new();
}