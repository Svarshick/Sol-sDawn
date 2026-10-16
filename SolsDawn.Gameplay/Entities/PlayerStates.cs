using System;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay.Entities;

// --- 1. STATE CATEGORY & MODEL ATTRIBUTES ---

[Flags]
public enum AllowedState
{
    Idle = 1 << 0,
    Run  = 1 << 1,
    Jump = 1 << 2,
    Fall = 1 << 3,
    All  = Idle | Run | Jump | Fall
}

public class StateCategoryAttribute : CategoryAttribute
{
    public AllowedState State { get; }

    public StateCategoryAttribute(AllowedState state, Type fallbackType)
        : base(fallbackType)
    {
        State = state;
    }

    public override string GetTagsDescription() => $"State: {State}";
}

public class StateModelAttribute : ModelAttribute
{
    public AllowedState States { get; }

    public StateModelAttribute(AllowedState states)
    {
        States = states;
    }

    public override bool Satisfies(CategoryAttribute baseConstraint)
    {
        if (baseConstraint is not StateCategoryAttribute constraint)
            return false;

        return (States & constraint.State) == constraint.State;
    }

    public override string GetTagsDescription() => $"States: {States}";
}

// --- 2. BASE PLAYER STATE & STATE MODELS ---

public abstract class PlayerState : Core.Logic.Gameplay.State<Player>;

public abstract class JumpState : PlayerState;

[StateModel(AllowedState.Idle)]
public class IdleState : PlayerState
{
    public override async Job Job(Player player)
    {
        player.Board.Velocity = Vector2.Zero;
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
    }
}

[StateModel(AllowedState.Run)]
public class RunState : PlayerState
{
    [Inspect] public MotionSolver Motion = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        Motion.ResetPhase();

        while (true)
        {
            var currentPlatform = board.CurrentPlatform;
            if (currentPlatform is null)
            {
                Log.Logic.Error("Platform can't be null while run");
                return;
            }

            var angle = currentPlatform.Transform.Rotation;
            var tangent = new Vector2(Cos(angle), Sin(angle));

            var inDir = Sign(Input.Move.X);
            var vx = Vector2.Dot(board.Velocity, tangent);

            vx = Motion.Calculate(vx, inDir, DeltaTime);

            board.Velocity = new Vector2(vx, 0).Rotated(angle);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Jump)]
public class CelesteJumpState : JumpState
{
    [Inspect(min: 0, max: PI / 2)] public float HorizontalDashAngle = PI / 6;
    [Inspect(min: 0)] public float HorizontalDashDistance = 5;
    [Inspect(min: 0)] public float VerticalDashDistance = 5;
    [Inspect(min: 1)] public int DashFrames = 3;

    [Inspect(min: 0)] public float HorizontalDashFinalSpeed = 20;
    [Inspect(min: 0)] public float VerticalDashFinalSpeed = 20;
    [Inspect(min: 0)] public float VerticalFriction = 15;

    [Inspect] public MotionSolver HorizontalMotion = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        HorizontalMotion.ResetPhase();

        {
            var initInDir = Sign(Input.Move.X);
            if (initInDir == 0)
            {
                var vy = VerticalDashDistance / (DashFrames * DeltaTime);
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = HorizontalDashDistance / (DashFrames * DeltaTime);
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }

            for (int i = 0; i < DashFrames; i++)
            {
                await NextFrame();
            }

            if (initInDir == 0)
            {
                var vy = VerticalDashFinalSpeed;
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = HorizontalDashFinalSpeed;
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while jump");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = HorizontalMotion.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = board.Velocity.Y - VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Jump)]
public class PrimitiveJumpState : JumpState
{
    [Inspect(min: 0)] public float HorizontalStartSpeed = 30;
    [Inspect(min: 0)] public float VerticalStartSpeed = 20;
    [Inspect(min: 0)] public float VerticalFriction = 15;

    [Inspect] public MotionSolver HorizontalMotion = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        HorizontalMotion.ResetPhase();

        {
            var inDir = Sign(Input.Move.X);
            board.Velocity = new Vector2(inDir * HorizontalStartSpeed, VerticalStartSpeed);
            await NextFrame();
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while jump");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = HorizontalMotion.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = board.Velocity.Y - VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

[StateModel(AllowedState.Fall)]
public class FallState : PlayerState
{
    [Inspect(min: 0)] public float VerticalAcceleration = 15;
    [Inspect(min: 0)] public float VerticalMaxSpeed = 30;

    [Inspect] public MotionSolver HorizontalMotion = new();

    public override async Job Job(Player player)
    {
        var board = player.Board;
        HorizontalMotion.ResetPhase();

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = HorizontalMotion.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = Max(-VerticalMaxSpeed, board.Velocity.Y - VerticalAcceleration * DeltaTime);

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}