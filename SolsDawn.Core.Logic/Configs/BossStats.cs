using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Configs;

public class BossStats
{
    public Color Color;
    public float Width;
    public float Height;

    public float HitDuration;
    public Color HitBlinkColor;

    public float BladeTelegraphDuration;
    public Color BladeTelegraphBlinkColor;
    public float BladeTelegraphStarDistance;
    public float BladeTelegraphStarDuration;
    public Color BladeTelegraphStarColor;
    public float BladeTelegraphStarOuterRadius;
    public float BladeTelegraphStarInnerRadius;
    [Euler] public float BladeTelegraphStarStartAngle;
    [Euler] public float BladeTelegraphStarDeltaAngle;
    public float BladeTelegraphStarThickness;

    public float BladeAttackDistance;
    [Euler] public float BladeAttackEdgeAngle;
    public float BladeAttackEdgeLength;
    public float BladeAttackEdgeWidth;
    public float BladeDashDistance;
    public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;
    public Color BladeTraceEndColor;

    public float BladeParriedDuration;
    public Color BladeParriedColor;
    public float BladeParriedPushDistance;
    public float BladeParriedPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireTelegraphDuration;
    public float FireTelegraphAimingDuration;
    public Color FireTelegraphBlinkColor;

    public float FireDistance;
    public float FireWidth;
    public float FireTraceDuration;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;

    public float FireParriedDuration;
    public Color FireParriedColor;
    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;

    public float TeleportTraceWidth;
    public float TeleportTraceDuration;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}