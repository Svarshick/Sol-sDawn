namespace SolsDawn.Gameplay.Entities;

public class IdleState(Player player) : State
{
    public override void Enter(State from)
    {
        player.Animator.Player.TryPlay(PlayerAnimations.Idle);
        player.Collider._body.LinearVelocity = Vector2.Zero;
    }
}

public class RunState(Player player) : State
{
    public enum Phase
    {
        Drive, //input != 0, accelerate/maintain speed 
        Brake, //input = 0, friction to stop
        Turn, //int != 0, accelerate in opposite direction
    }

    public override async Job Job()
    {
        var specs = player.Board.Specs.Run;
        var body = player.Collider._body;
        var phase = Phase.Drive;
        
        while (true)
        {
            var angle = player.Board.CurrentPlatform!.Transform.Rotation;
            var inDir = (int)Input.Move.X;
            var vx = Vector2.Dot(body.LinearVelocity, new Vector2(Cos(angle), Sin(angle)));
            var vxAbs = Abs(vx);
            var vxDir = Sign(vx);
            
            var greatSpeed = vxAbs > specs.Speed;
            phase = inDir switch
            {
                0 => Phase.Brake,
                _ when phase == Phase.Turn || inDir == -vxDir => Phase.Turn,
                _ => Phase.Drive
            };

            switch (greatSpeed, phase)
            {
                case (greatSpeed: true, Phase.Drive):
                {
                    var excess = vxAbs - specs.Speed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach < DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.Speed;
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, Phase.Brake):
                {
                    var excess = vxAbs - specs.Speed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach < DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.Speed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.Friction * timeRemains);
                    }
                    
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: true, Phase.Turn):
                {
                    var excess = vxAbs - specs.Speed;
                    var timeToReach = excess / specs.GreatSpeedFriction;
                    if (timeToReach < DeltaTime)
                    {
                        vxAbs -= specs.GreatSpeedFriction * DeltaTime;
                    }
                    else
                    {
                        vxAbs = specs.Speed;
                        var timeRemains = DeltaTime - timeToReach;
                        vxAbs = Max(0, vxAbs - specs.TurnAcceleration * timeRemains);
                    }

                    vx = vxDir * vxAbs;
                    break;
                }
                
                case(greatSpeed: false, Phase.Drive):
                {
                    vxAbs = Min(specs.Speed, vxAbs + specs.Acceleration * DeltaTime);
                    vx = inDir * vxAbs;
                    break;
                }
                    
                case (greatSpeed: false, Phase.Brake):
                {
                    vxAbs = Max(0, vxAbs - specs.Friction * DeltaTime);
                    vx = vxDir * vxAbs;
                    break;
                }
                
                case (greatSpeed: false, Phase.Turn):
                {
                    vx += inDir * specs.TurnAcceleration;
                    if (Abs(vx) >= specs.Speed)
                    {
                        vx = inDir * specs.Speed;
                        phase = Phase.Drive;
                    }
                    break;
                }
            }

            body.LinearVelocity = new Vector2(vx, 0).Rotated(angle);
            await NextFrame();
        }
    }
}

public class CelesteJumpState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.CelesteJump;
        var body = player.Collider._body;

        Vector2 dashVelocity;
        var dashSec = FpsSec * specs.DashFrames;

        if (Input.Move.X == 0)
        {
            dashVelocity = new (0, specs.VerticalDashDistance / dashSec);
        }
        else
        {
            var xSign = Input.Move.X > 0 ? 1 : -1;
            var xVelocity = xSign * specs.HorizontalDashDistance / dashSec;
            var dashRadians = specs.HorizontalDashAngle * PI / 180;
            var yVelocity  = specs.HorizontalDashDistance * Tan(dashRadians) / dashSec;
            dashVelocity = new Vector2(xVelocity, yVelocity);
        }

        body.LinearVelocity = dashVelocity;
        
        for (var i = 0; i < specs.DashFrames; i++)
        {
            await NextFrame();
        }
        
        player.Enter(new FallState(player));
    }
}

public class PrimitiveJumpState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.PrimitiveJump;
        var body = player.Collider._body;
        
        var direction = Input.Move;
        body.LinearVelocity = new Vector2(direction.X * specs.HorizontalStartSpeed, specs.VerticalStartSpeed);
        
        while (body.LinearVelocity.Y >= 0)
        {
            direction = Input.Move;
            var targetVx = direction.X * specs.HorizontalSpeed;
            var currentVx = body.LinearVelocity.X;

            if (Abs(currentVx) >= specs.HorizontalSpeed)
            {
                if (currentVx * direction.X < 0)
                    currentVx += targetVx;
            }
            else
            {
                currentVx = targetVx;
            }

            body.LinearVelocity = new Vector2(currentVx, body.LinearVelocity.Y);
            body.ApplyForce(new Vector2(0, specs.VerticalAcceleration));
            await NextFrame();
        }

        player.Enter(new FallState(player));
    }
}

public class FallState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs.Fall;
        var body = player.Collider._body;
        
        while (!player.Board.OnGround)
        {
            var direction = Input.Move;
            
            var targetVx = direction.X * specs.FallHorizontalSpeed;
            var currentVx = body.LinearVelocity.X;
            if (Abs(currentVx) >= specs.FallHorizontalSpeed)
            {
                if (currentVx * direction.X < 0)
                    currentVx += targetVx;
            }
            else
            {
                currentVx = targetVx;
            }
            
            var currentVy = body.LinearVelocity.Y;
            if (body.LinearVelocity.Y > -specs.FallVerticalSpeedLimit)
            {
                body.ApplyForce(new Vector2(0, -specs.FallVerticalAcceleration));
            }
            else
            {
                currentVy = -specs.FallVerticalSpeedLimit;
            }
            
            body.LinearVelocity = new Vector2(currentVx, currentVy);
            await NextFrame();
        }
        
        player.Enter(new IdleState(player));
    }
}