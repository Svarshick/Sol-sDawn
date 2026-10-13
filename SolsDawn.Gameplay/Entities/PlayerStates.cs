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
            var inDir = Sign(Input.Move.X);
            var vx = Vector2.Dot(board.Velocity, new Vector2(Cos(angle), Sin(angle)));
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.ControlledSpeed;
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
                    var excess = vxAbs - specs.ControlledSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.ControlledSpeed;
                    }

                    vx = inDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, RunPhase.Brake):
                {
                    var excess = vxAbs - specs.ControlledSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.ControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.Friction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, RunPhase.Turn):
                {
                    var excess = vxAbs - specs.ControlledSpeed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.ControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.TurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, RunPhase.Drive):
                {
                    vxAbs = Min(specs.ControlledSpeed, vxAbs + specs.Acceleration * DeltaTime);
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
                    if (Abs(vx) >= specs.ControlledSpeed)
                    {
                        vx = inDir * specs.ControlledSpeed;
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

public enum CelesteJumpPhase
{
    Drive, //input != 0, accelerate/maintain speed 
    Brake, //input = 0, friction to stop
    Turn, //int != 0, accelerate in opposite direction
}

public class CelesteJumpState(Player player) : JumpState
{
    public CelesteJumpPhase Phase { get; private set; }
    
    public override async Job Job()
    {
        var specs = player.Board.Specs.CelesteJump;
        var board = player.Board;
        Phase = CelesteJumpPhase.Drive;
        
        {
            var initInDir = Sign(Input.Move.X);
            if (initInDir == 0)
            {
                var vy = specs.VerticalDashDistance / (specs.DashFrames * DeltaTime);
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = specs.HorizontalDashDistance / (specs.DashFrames * DeltaTime);
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(specs.HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }

            for (int i = 0; i < specs.DashFrames; i++)
            {
                await NextFrame();
            }

            if (initInDir == 0)
            {
                var vy = specs.VerticalDashFinalSpeed;
                board.Velocity = new Vector2(0, vy);
            }
            else
            {
                var vxAbs = specs.HorizontalDashFinalSpeed;
                var vx = initInDir * vxAbs;
                var vy = vxAbs * Tan(specs.HorizontalDashAngle);
                board.Velocity = new Vector2(vx, vy);
            }
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = board.Velocity.X;
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.HorizontalControlledSpeed;
            Phase = inDir switch
            {
                0 => CelesteJumpPhase.Brake,
                _ when Phase == CelesteJumpPhase.Turn || inDir == -vxDir => CelesteJumpPhase.Turn,
                _ => CelesteJumpPhase.Drive
            };
            
            switch (greatSpeed, Phase)
            {
                case (greatSpeed: true, CelesteJumpPhase.Drive):
                {
                    break;
                }
                
                case (greatSpeed: true, CelesteJumpPhase.Brake):
                {
                    var excess = vxAbs - specs.HorizontalControlledSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalFriction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, CelesteJumpPhase.Turn):
                {
                    var excess = vxAbs - specs.HorizontalControlledSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalTurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, CelesteJumpPhase.Drive):
                {
                    vxAbs = Min(specs.HorizontalControlledSpeed, vxAbs + specs.HorizontalAcceleration * DeltaTime);
                    vx = inDir * vxAbs;
                    break;
                }
                    
                case (greatSpeed: false, CelesteJumpPhase.Brake):
                {
                    vxAbs = Max(0, vxAbs - specs.HorizontalFriction * DeltaTime);
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: false, CelesteJumpPhase.Turn):
                {
                    vx += inDir * specs.HorizontalTurnAcceleration;
                    if (Abs(vx) >= specs.HorizontalControlledSpeed)
                    {
                        vx = inDir * specs.HorizontalControlledSpeed;
                        Phase = CelesteJumpPhase.Drive;
                    }
                    break;
                }
            }
            
            var vy = board.Velocity.Y - specs.VerticalFriction * DeltaTime;
            board.Velocity = new Vector2(vx, vy);
            await NextFrame();
        }
    }
}

public enum PrimitiveJumpPhase
{
    Drive, //input != 0, accelerate/maintain speed 
    Brake, //input = 0, friction to stop
    Turn, //int != 0, accelerate in opposite direction
}

public class PrimitiveJumpState(Player player) : JumpState
{
    public PrimitiveJumpPhase Phase;
    
    public override async Job Job()
    {
        var specs = player.Board.Specs.PrimitiveJump;
        var board = player.Board;
        {
            Phase = PrimitiveJumpPhase.Drive;
            var inDir = Sign(Input.Move.X);
            board.Velocity = new Vector2(inDir * specs.HorizontalStartSpeed, specs.VerticalStartSpeed);
            await NextFrame();
        }

        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = board.Velocity.X;
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.HorizontalControlledSpeed;
            Phase = inDir switch
            {
                0 => PrimitiveJumpPhase.Brake,
                _ when Phase == PrimitiveJumpPhase.Turn || inDir == -vxDir => PrimitiveJumpPhase.Turn,
                _ => PrimitiveJumpPhase.Drive
            };
            
            switch (greatSpeed, Phase)
            {
                case (greatSpeed: true, PrimitiveJumpPhase.Drive):
                {
                    break;
                }
                
                case (greatSpeed: true, PrimitiveJumpPhase.Brake):
                {
                    var excess = vxAbs - specs.HorizontalControlledSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalFriction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, PrimitiveJumpPhase.Turn):
                {
                    var excess = vxAbs - specs.HorizontalControlledSpeed;
                    var timeToReach = excess / specs.HorizontalGreatSpeedFriction;
                    if (timeToReach > DeltaTime)
                    {
                        vxAbs -= specs.HorizontalGreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.HorizontalControlledSpeed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.HorizontalTurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, PrimitiveJumpPhase.Drive):
                {
                    vxAbs = Min(specs.HorizontalControlledSpeed, vxAbs + specs.HorizontalAcceleration * DeltaTime);
                    vx = inDir * vxAbs;
                    break;
                }
                    
                case (greatSpeed: false, PrimitiveJumpPhase.Brake):
                {
                    vxAbs = Max(0, vxAbs - specs.HorizontalFriction * DeltaTime);
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: false, PrimitiveJumpPhase.Turn):
                {
                    vx += inDir * specs.HorizontalTurnAcceleration;
                    if (Abs(vx) >= specs.HorizontalControlledSpeed)
                    {
                        vx = inDir * specs.HorizontalControlledSpeed;
                        Phase = PrimitiveJumpPhase.Drive;
                    }
                    break;
                }
            }
            
            var vy = board.Velocity.Y - specs.VerticalFriction * DeltaTime;
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
        Phase = FallPhase.Drive;
        
        while (true)
        {
            if (board.CurrentPlatform is not null)
            {
                Log.Logic.Error("Can't be on platform while fall");
                return;
            }

            var inDir = Sign(Input.Move.X);
            var vx = board.Velocity.X;
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.HorizontalMaxSpeed;
            Phase = inDir switch
            {
                0 => FallPhase.Brake,
                _ when Phase == FallPhase.Turn || inDir == -vxDir => FallPhase.Turn,
                _ => FallPhase.Drive
            };

            switch (greatSpeed, phase: Phase)
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
                        Phase = FallPhase.Drive;
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