using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay.Entities;

public class IdleState(Player player) : State
{
    public override async Job Job()
    {
        player.Board.Velocity = Vector2.Zero;
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
    }
}

public class RunState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.Run;
        var board = player.Board;
        // Uses the Run-specific motion solver!
        var motionSolver = specs.MotionSolver; 

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

            vx = motionSolver.Calculate(vx, inDir, DeltaTime);

            board.Velocity = new Vector2(vx, 0).Rotated(angle);
            await NextFrame();
        }
    }
}

public abstract class JumpState : State;

public class CelesteJumpState(Player player) : JumpState
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.CelesteJump;
        var board = player.Board;

        {
            // (Initial jump velocity math omitted for brevity, remains unchanged)
        }

        // Uses the CelesteJump-specific horizontal motion solver!
        var horizontalMotionSolver = specs.HorizontalMotionSolver; 
        
        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = horizontalMotionSolver.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = board.Velocity.Y - specs.VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

public class PrimitiveJumpState(Player player) : JumpState
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.PrimitiveJump;
        var board = player.Board;

        {
            var inDir = Sign(Input.Move.X);
            // Uses the HorizontalMotionSolver's ControlledSpeed as the start speed if desired, or keep as is.
            board.Velocity = new Vector2(inDir * specs.HorizontalMotionSolver.ControlledSpeed, specs.VerticalStartSpeed);
            await NextFrame();
        }

        // Uses the PrimitiveJump-specific horizontal motion solver!
        var horizontalMotionSolver = specs.HorizontalMotionSolver; 
        
        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = horizontalMotionSolver.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = board.Velocity.Y - specs.VerticalFriction * DeltaTime;

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

public class FallState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.Fall;
        var board = player.Board;
        
        // Uses the Fall-specific horizontal motion solver!
        var horizontalMotionSolver = specs.HorizontalMotionSolver; 

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = horizontalMotionSolver.Calculate(board.Velocity.X, inDir, DeltaTime);
            var vy = Max(-specs.VerticalMaxSpeed, board.Velocity.Y - specs.VerticalAcceleration * DeltaTime);

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}