using System;

namespace SolsDawn.Gameplay.Entities;

public class IdleState(Player player) : State
{
    public override void Enter(State from)
    {
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
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
                Direction * player.Board.Specs.Velocity * ElapsedSeconds;
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
        _teleportLine = new(player.GameObject.Transform.Position, endPosition, player.Board.Specs.TeleportWidth,
            player.Board.Specs.TeleportStartColor);
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
            _teleportLine.Color = Color.Lerp(player.Board.Specs.TeleportStartColor, player.Board.Specs.TeleportEndColor, lerp);
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
            player.Board.Specs.TeleportTraceWidth,
            player.Board.Specs.TeleportTraceDuration,
            player.Board.Specs.TeleportTraceColor));

        player.GameObject.Transform.Position = endPosition;
        player.Board.LastTeleportUsage = TotalSeconds;
    }

    private float TeleportLerp(double elapsedTime)
    {
        return MathHelper.Clamp((float)(elapsedTime / player.Board.Specs.TeleportHoldDuration), 0f, 1f);
    }

    private Vector2 TeleportPosition(Vector2 pointPosition, float lerp)
    {
        var teleportDirection = pointPosition - player.GameObject.Transform.Position;
        teleportDirection.Normalize();
        var delta = lerp * (player.Board.Specs.TeleportMaxDistance - player.Board.Specs.TeleportMinDistance);
        return player.GameObject.Transform.Position + teleportDirection * (player.Board.Specs.TeleportMinDistance + delta);
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
            player.Board.Specs.BladeDashDistance,
            player.Board.Specs.BladeDashWidth,
            player.Board.Specs.BladeAttackDistance,
            player.Board.Specs.BladeAttackWidth);

        var shape = Shapes.PolygonShape(vertices);
        var atk = Fight.PlayerBladeParryingAttack(
            shape,
            player.GameObject.Transform.Position,
            direction.Angle(),
            null,
            async _ =>
            {
                Console.WriteLine("player attack"); 
            },
            async _ =>
            {
                Console.WriteLine("player parry");
                player.Enter(new IdleState(player));
            });

        atk.Open();
        await NextFrame();
        Game.AnimationsPool.Add(
            new ArrowPentagonTraceAnimation(
                player.Board.Specs.BladeTraceDuration,
                player.GameObject.Transform.Position,
                direction,
                player.Board.Specs.BladeDashDistance,
                player.Board.Specs.BladeDashWidth,
                player.Board.Specs.BladeAttackDistance,
                player.Board.Specs.BladeAttackWidth,
                player.Board.Specs.BladeTraceColor
            ));
        player.GameObject.Transform.Position += direction * player.Board.Specs.BladeDashDistance;
        atk.Destroy();
        player.Enter(new IdleState(player));
    }
}

public class FireState(
    Player player,
    Vector2 direction)
    : State
{
    public override async Job Update()
    {
        var fromPosition = player.Transform.Position;
        var toPosition = fromPosition + direction * player.Board.Specs.FireDistance;
        var width = player.Board.Specs.FireWidth;
        var fireDuration = 0.5f;
        var fireColor = player.Board.Specs.FireTraceColor;
        var parryTraceDuration = player.Board.Specs.FireParryTraceDuration;
        var parryTraceColor = player.Board.Specs.FireParryTraceColor;

        if (Fight.FireParryCast(
                fromPosition,
                toPosition,
                width,
                out var result))
        {
            var parryContext = new FireParryContext((fromPosition + result.Position) / 2);
            result.ParryWindow.Execute(
                parryContext,
                async context =>
                {
                    Game.AnimationsPool.Add(new LineTraceAnimation(
                        fromPosition,
                        context.BumpPoint,
                        width,
                        parryTraceDuration,
                        parryTraceColor));
                });
        }
        else
        {
            Animations.LineTrace(
                fromPosition,
                toPosition,
                width,
                fireDuration,
                fireColor);
            
            await Timer(fireDuration);
        }
        
        player.Enter(new IdleState(player));
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

        player.Enter(new IdleState(player));
    }
}