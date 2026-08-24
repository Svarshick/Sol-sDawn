using System;

namespace SolsDawn.Gameplay.Entities;

public class PlayerBoard
{
    public float LastBladeUsage;
    public float LastTeleportUsage;
    public float LastFireUsage;
    public bool BladeCharged => TotalSeconds - LastBladeUsage >= Config.BladeRechargeDuration;
    public bool TeleportCharged => TotalSeconds - LastTeleportUsage >= Config.TeleportRechargeDuration;
    public bool FireCharged => TotalSeconds - LastFireUsage >= Config.FireRechargeDuration;

    public PlayerConfig Config = new();
}

public class PlayerConfig
{

    public float TeleportRechargeDuration;
    public float BladeRechargeDuration;
    public float FireRechargeDuration;

    public Color Color;
    public float Width;
    public float Height;
    public float Velocity;
    public float CursorRadius;
    public Color CursorColor;

    public float HitInvulnerabilityDuration;
    public Color HitBlinkColor;

    public float BladeAttackDistance;
    public float BladeAttackWidth;
    public float BladeDashDistance;
    public float BladeDashWidth;
    public float BladeTraceDuration;
    public Color BladeTraceStartColor;

    public float BladeParryPushDistance;
    public float BladeParryPushVelocity;
    public float BladeParryTraceDuration;
    public Color BladeParryTraceStartColor;
    public Color BladeParryTraceEndColor;

    public float FireDistance;
    public float FireWidth;
    public float FireTraceDuration;
    public float FireTraceWidth;
    public Color FireTraceStartColor;
    public Color FireTraceEndColor;

    public float FireParryTraceDuration;
    public Color FireParryTraceStartColor;
    public Color FireParryTraceEndColor;

    public float TeleportMinDistance;
    public float TeleportMaxDistance;
    public float TeleportHoldDuration;
    public float TeleportWidth;
    public Color TeleportStartColor;
    public Color TeleportEndColor;
    public float TeleportTraceWidth;
    public float TeleportTraceDuration;
    public Color TeleportTraceStartColor;
    public Color TeleportTraceEndColor;
}

public class IdleState(Player player) : State
{
    public override void Enter(State from)
    {
        player.Animator.Player.TryPlay(DefaultAnimation.Idle);
    }
}

public class MoveState(Player player) : State
{
    public Vector2 Direction;

    public override void Enter(State from)
    {
        Direction = Input.Move;
    }

    public override async Job Update()
    {
        while (true)
        {
            Direction = Input.Move;
            player.GameObject.Transform.Position +=
                Direction * player.Board.Config.Velocity * ElapsedSeconds;
            await NextFrame();
        }
    }
}

public class TeleportState(Player player) : State
{
    public Vector2 ScreenPosition = Input.Teleport.ScreenPosition;
    public double ElapsedTime;

    private LineIdleAnimation _teleportLine;

    public override void Enter(State from)
    {
        ScreenPosition = Input.Teleport.ScreenPosition;
        var mousePosition = Game.Camera.ScreenToWorld(ScreenPosition);
        var endPosition = TeleportPosition(mousePosition, 0);
        _teleportLine = new(player.GameObject.Transform.Position, endPosition, player.Board.Config.TeleportWidth,
            player.Board.Config.TeleportStartColor);
        Game.AnimationsPool.Add(_teleportLine);
    }

    public override async Job Update()
    {
        while (true)
        {
            ScreenPosition = Input.Teleport.ScreenPosition;
            ElapsedTime += ElapsedSeconds;
            var mousePosition = Game.Camera.ScreenToWorld(ScreenPosition);
            var lerp = TeleportLerp(ElapsedTime);
            _teleportLine.Transform.Position = player.GameObject.Transform.Position;
            _teleportLine.End = TeleportPosition(mousePosition, lerp);
            _teleportLine.Color = Color.Lerp(player.Board.Config.TeleportStartColor, player.Board.Config.TeleportEndColor, lerp);
            await NextFrame();
        }
    }

    public override void Exit(State to)
    {
        ScreenPosition = Input.Teleport.ScreenPosition;
        _teleportLine.Kill();
        var mousePosition = Game.Camera.ScreenToWorld(ScreenPosition);
        var lerp = TeleportLerp(ElapsedTime);
        var endPosition = TeleportPosition(mousePosition, lerp);

        Game.AnimationsPool.Add(new LineTraceAnimation(
            player.GameObject.Transform.Position,
            endPosition,
            player.Board.Config.TeleportTraceWidth,
            player.Board.Config.TeleportTraceDuration,
            player.Board.Config.TeleportTraceStartColor));

        player.GameObject.Transform.Position = endPosition;
        player.Board.LastTeleportUsage = TotalSeconds;
    }

    private float TeleportLerp(double elapsedTime)
    {
        return MathHelper.Clamp((float)(elapsedTime / player.Board.Config.TeleportHoldDuration), 0f, 1f);
    }

    private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
    {
        var teleportDirection = pointPosition - player.GameObject.Transform.Position;
        teleportDirection.Normalize();
        var delta = lerp * (player.Board.Config.TeleportMaxDistance - player.Board.Config.TeleportMinDistance);
        return player.GameObject.Transform.Position + teleportDirection * (player.Board.Config.TeleportMinDistance + delta);
    }
}

public class BladeState(
    Player player,
    Vector2 lookPosition)
    : State
{
    public override async Job Update()
    {
        var direction = lookPosition - player.GameObject.Transform.Position;
        direction.Normalize();
        var vertices = Helper.ArrowPentagonVertices(
            Vector2.Zero,
            new Vector2(1, 0),
            player.Board.Config.BladeDashDistance,
            player.Board.Config.BladeDashWidth,
            player.Board.Config.BladeAttackDistance,
            player.Board.Config.BladeAttackWidth);

        var shape = Shapes.PolygonShape(vertices);
        var parry = false;
        var atk = PlayerAttack(
            shape,
            player.GameObject.Transform.Position,
            direction.Angle(),
            async _ => Console.WriteLine("hit"),
            _ => true,
            async _ =>
            {
                parry = true;
                Console.WriteLine("parry");
                var intention = new EnterStateIntention(player.GameObject, new SlideState(player, -direction, 20, 0.05f));
                IntentionsPool.AddIntention(intention);
            });

        atk.Start();

        await NextFrame();
        atk.End();
        if (!parry)
        {
            Game.AnimationsPool.Add(
                new ArrowPentagonTraceAnimation(
                    player.Board.Config.BladeTraceDuration,
                    player.GameObject.Transform.Position,
                    direction,
                    player.Board.Config.BladeDashDistance,
                    player.Board.Config.BladeDashWidth,
                    player.Board.Config.BladeAttackDistance,
                    player.Board.Config.BladeAttackWidth,
                    player.Board.Config.BladeTraceStartColor
                ));

            player.GameObject.Transform.Position += direction * player.Board.Config.BladeDashDistance;
        }

        var intention = new EnterStateIntention(player.GameObject, new IdleState(player));
        IntentionsPool.AddIntention(intention);
    }
}

public class SlideState(
    Player player,
    Vector2 direction,
    float speed,
    float time)
    : State
{
    public override async Job Update()
    {
        var startTime = TotalSeconds;
        while (TotalSeconds - startTime < time)
        {
            player.GameObject.Transform.Position += direction * speed * ElapsedSeconds;
            await NextFrame();
        }

        var intention = new EnterStateIntention(player.GameObject, new IdleState(player));
        IntentionsPool.AddIntention(intention);
    }
}

/*
public class FireExecuteState(
    Player player,
    Vector2 lookPosition,
    IReadOnlyList<GameObject> targets)
    : State
{
    public readonly IReadOnlyList<GameObject> Targets = targets;
    public readonly Vector2 LookPosition = lookPosition;

    public override void Enter(State from)
    {
        var direction = LookPosition - player.GameObject.Transform.Position;
        direction.Normalize();
        var fireEnd = player.GameObject.Transform.Position + direction * player.Stats.FireDistance;

        Game.AnimationsPool.Add(new LineTrace(
            new Transform2 { Position = player.GameObject.Transform.Position },
            fireEnd,
            player.Stats.FireTraceWidth,
            player.Stats.FireTraceDuration,
            player.Stats.FireTraceStartColor,
            player.Stats.FireTraceEndColor));

        if (Targets.Count > 0)
        {
            AffectsPool.Add(new DamageAffect(player.GameObject, Targets, 1));
        }

        var pending = new IdleState(player);
        IntentionsPool.AddIntention(Intend(player.GameObject, pending));
    }
}*/