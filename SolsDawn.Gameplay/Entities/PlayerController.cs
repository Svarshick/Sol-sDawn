namespace SolsDawn.Gameplay.Entities;

public class PlayerController
{
    private readonly Player _player;
    
    public PlayerController(Player player)
    {
        _player = player;
    }

    public void Update()
    {
        if (_player.Board.OnGround)
        {
            if (Input.Jump)
            {
                _player.Enter(new JumpState(_player));
                return;
            }
            
            if (_player.State is RunState &&
                Input.Move == Vector2.Zero)
            {
                var idleState = new IdleState(_player);
                _player.Enter(idleState);
                return;
            }

            if (_player.State is IdleState &&
                Input.Move != Vector2.Zero)
            {
                var moveState = new RunState(_player);
                _player.Enter(moveState);
                return;
            }
        }
        else
        {
            if (_player.State is not (FallState or JumpState))
                _player.Enter(new FallState(_player));
        }
    }
}