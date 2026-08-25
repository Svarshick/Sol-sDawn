namespace SolsDawn.Gameplay.Entities;

public record PlayerBoard
{
    public float LastBladeUsage;
    public float LastTeleportUsage;
    public float LastFireUsage;
    public bool BladeCharged => TotalSeconds - LastBladeUsage >= Config.BladeRechargeDuration;
    public bool TeleportCharged => TotalSeconds - LastTeleportUsage >= Config.TeleportRechargeDuration;
    public bool FireCharged => TotalSeconds - LastFireUsage >= Config.FireRechargeDuration;

    public PlayerConfig Config = new();
}

public record PlayerConfig
{
    public float TeleportRechargeDuration = 1;
    public float BladeRechargeDuration = 1;
    public float FireRechargeDuration = 1;

    public Color Color = Color.Blue;
    public float Width = 0.7f;
    public float Height = 1.3f;
    public float Velocity = 6;

    public float HitInvulnerabilityDuration = 1;
    public Color HitBlinkColor = Color.Red;

    public float BladeAttackDistance = 1;
    public float BladeAttackWidth = 2;
    public float BladeDashDistance = 2;
    public float BladeDashWidth = 1;
    public float BladeTraceDuration = 0.5f;
    public Color BladeTraceColor = Color.DarkGoldenrod;

    public float BladeParryPushDistance;
    public float BladeParryPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireDistance;
    public float FireWidth;
    public float FireTraceDuration;
    public float FireTraceWidth;
    public Color FireTraceColor = Color.Fuchsia;

    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;

    public float TeleportMinDistance = 4;
    public float TeleportMaxDistance = 9;
    public float TeleportHoldDuration = 0.6f;
    public float TeleportWidth = 0.3f;
    public Color TeleportStartColor = Color.Transparent;
    public Color TeleportEndColor = Color.Aqua;
    public float TeleportTraceWidth = 1;
    public float TeleportTraceDuration = 1;
    public Color TeleportTraceColor = Color.Aqua;
}