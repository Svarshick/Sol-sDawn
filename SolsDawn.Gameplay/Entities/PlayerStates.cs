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
    public Vector2 Direction;

    public override async Job Job()
    {
        var specs = player.Board.Specs;
        while (true)
        {
            Direction = Input.Move.Rotated(player.Board.CurrentPlatform!.Transform.Rotation);
            player.Collider._body.LinearVelocity = Direction * specs.RunSpeed;
            await NextFrame();
        }
    }
}

public class JumpState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs;
        var body = player.Collider._body;
        
        var direction = Input.Move;
        body.LinearVelocity = new Vector2(direction.X * specs.JumpHorizontalStartSpeed, specs.JumpVerticalStartSpeed);
        
        while (body.LinearVelocity.Y >= 0)
        {
            direction = Input.Move;
            var targetVx = direction.X * specs.JumpHorizontalSpeed;
            var currentVx = body.LinearVelocity.X;

            if (Abs(currentVx) >= specs.JumpHorizontalSpeed)
            {
                if (currentVx * direction.X < 0)
                    currentVx += targetVx;
            }
            else
            {
                currentVx = targetVx;
            }

            body.LinearVelocity = new Vector2(currentVx, body.LinearVelocity.Y);
            body.ApplyForce(new Vector2(0, specs.JumpVerticalAcceleration));
            await NextFrame();
        }

        player.Enter(new FallState(player));
    }
}

public class FallState(Player player) : State
{
    public override async Job Job()
    {
        var specs = player.Board.Specs;
        var body = player.Collider._body;
        
        while (!player.Board.OnGround)
        {
            var direction = Input.Move;
            
            var targetVx = direction.X * specs.JumpHorizontalSpeed;
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
            if (Abs(body.LinearVelocity.Y) < Abs(specs.FallVerticalSpeedLimit))
            {
                body.ApplyForce(new Vector2(0, specs.FallVerticalAcceleration));
            }
            else
            {
                currentVy = specs.FallVerticalSpeedLimit;
            }
            
            body.LinearVelocity = new Vector2(currentVx, currentVy);
            await NextFrame();
        }
        
        player.Enter(new IdleState(player));
    }
}