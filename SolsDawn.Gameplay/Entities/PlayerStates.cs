using SolsDawn.Core.Logic;
using SolsDawn.Core.Logic.Gameplay;

namespace SolsDawn.Gameplay.Entities;

public class IdleState(Player player) : State
{
    public override async Job Job()
    {
        player.Board.Velocity = Vector2.Zero;
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
    }
}

public enum RunPhase
{
    Drive, //input != 0, accelerate/maintain speed 
    Brake, //input = 0, friction to stop
    Turn, //int != 0, accelerate in opposite direction
}

public class RunState(Player player) : State
{
    public RunPhase Phase { get; private set; }

    public override async Job Job()
    {
        var specs = player.Board.Specs.Run;
        var board = player.Board;
        Phase = RunPhase.Drive;
        
        while (true)
        {
            var currentPlatform = player.Board.CurrentPlatform;
            if (currentPlatform is null)
            {
                Log.Logic.Error("Platform can't be null while run");
                return;
            }

            var angle = player.Board.CurrentPlatform!.Transform.Rotation;
            var inDir = (int)GameplayAPI.Input.Move.X;
            var vx = Vector2.Dot(board.Velocity, new Vector2(Cos(angle), Sin(angle)));
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.MaxSpeed;
            Phase = inDir switch
            {
                0 => RunPhase.Brake,
                _ when Phase == RunPhase.Turn || inDir == -vxDir => RunPhase.Turn,
                _ => RunPhase.Drive
            };

            switch (greatSpeed, Phase)
            {
                case (greatSpeed: true, RunPhase.Drive):
                {
                    var excess = vxAbs - specs.MaxSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.MaxSpeed;
                    }

                    vx = inDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, RunPhase.Brake):
                {
                    var excess = vxAbs - specs.MaxSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.MaxSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.Friction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, RunPhase.Turn):
                {
                    var excess = vxAbs - specs.MaxSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.MaxSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.TurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, RunPhase.Drive):
                {
                    vxAbs = Min(specs.MaxSpeed, vxAbs + specs.Acceleration * DeltaTime);
                    vx = inDir * vxAbs;
                    break;
                }
                    
                case (greatSpeed: false, RunPhase.Brake):
                {
                    vxAbs = Max(0, vxAbs - specs.Friction * DeltaTime);
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: false, RunPhase.Turn):
                {
                    vx += inDir * specs.TurnAcceleration;
                    if (Abs(vx) >= specs.MaxSpeed)
                    {
                        vx = inDir * specs.MaxSpeed;
                        Phase = RunPhase.Drive;
                    }
                    break;
                }
            }

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

        Vector2 dashVelocity;
        var dashSec = FpsSec * specs.DashFrames;

        if (GameplayAPI.Input.Move.X == 0)
        {
            dashVelocity = new (0, specs.VerticalDashDistance / dashSec);
        }
        else
        {
            var xSign = GameplayAPI.Input.Move.X > 0 ? 1 : -1;
            var xVelocity = xSign * specs.HorizontalDashDistance / dashSec;
            var dashRadians = specs.HorizontalDashAngle * PI / 180;
            var yVelocity  = specs.HorizontalDashDistance * Tan(dashRadians) / dashSec;
            dashVelocity = new Vector2(xVelocity, yVelocity);
        }

        board.Velocity = dashVelocity;
        
        for (var i = 0; i < specs.DashFrames; i++)
        {
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
        
        var inDir = Sign(GameplayAPI.Input.Move.X);
        board.Velocity = new Vector2(inDir * specs.HorizontalStartSpeed, specs.VerticalStartSpeed);
        await NextFrame();
        while (true)
        {
            inDir = Sign(GameplayAPI.Input.Move.X);
            var targetVx = inDir * specs.HorizontalSpeed;
            var vx = board.Velocity.X;

            if (Abs(vx) >= specs.HorizontalSpeed)
            {
                if (vx * inDir < 0)
                    vx += targetVx;
            }
            else
            {
                vx = targetVx;
            }

            var vy = board.Velocity.Y - specs.VerticalAcceleration * DeltaTime;
            
            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

public enum FallPhase
{
    Drive, //input != 0, accelerate/maintain speed 
    Brake, //input = 0, friction to stop
    Turn, //int != 0, accelerate in opposite direction
}

public class FallState(Player player) : State
{
    public FallPhase Phase { get; private set; }
    

    public override async Job Job()
    {
        var specs = player.Board.Specs.Fall;
        var board = player.Board;
        var phase = FallPhase.Drive;
        
        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = (int)GameplayAPI.Input.Move.X;
            var vx = board.Velocity.X;
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.HorizontalMaxSpeed;
            phase = inDir switch
            {
                0 => FallPhase.Brake,
                _ when phase == FallPhase.Turn || inDir == -vxDir => FallPhase.Turn,
                _ => FallPhase.Drive
            };

            switch (greatSpeed, phase)
            {
                case (greatSpeed: true, FallPhase.Drive):
                {
                    var excess = vxAbs - specs.HorizontalMaxSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalMaxSpeed;
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, FallPhase.Brake):
                {
                    var excess = vxAbs - specs.HorizontalMaxSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalMaxSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalFriction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, FallPhase.Turn):
                {
                    var excess = vxAbs - specs.HorizontalMaxSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalMaxSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalTurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, FallPhase.Drive):
                {
                    vxAbs = Min(specs.HorizontalMaxSpeed, vxAbs + specs.HorizontalAcceleration * DeltaTime);
                    vx = inDir * vxAbs;
                    break;
                }
                    
                case (greatSpeed: false, FallPhase.Brake):
                {
                    vxAbs = Max(0, vxAbs - specs.HorizontalFriction * DeltaTime);
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: false, FallPhase.Turn):
                {
                    vx += inDir * specs.HorizontalTurnAcceleration;
                    if (Abs(vx) >= specs.HorizontalMaxSpeed)
                    {
                        vx = inDir * specs.HorizontalMaxSpeed;
                        phase = FallPhase.Drive;
                    }
                    break;
                }
            }

            var vy = board.Velocity.Y;
            if (vy - specs.VerticalAcceleration * DeltaTime < -specs.VerticalMaxSpeed)
            {
                vy = -specs.VerticalMaxSpeed;
            }
            else
            {
                vy -= specs.VerticalAcceleration * DeltaTime;
            }

            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}