using SolsDawn.Core.Logic;

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
                _player.Enter(CreateJumpState());
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
            if (_player.State is not (FallState or PrimitiveJumpState))
                _player.Enter(new FallState(_player));
        }
    }

    private State CreateJumpState() => 
        Debug.CurrentJumpHeuristic.Name switch
        {
            nameof(PrimitiveJumpState) => new PrimitiveJumpState(_player),
            nameof(CelesteJumpState) => new CelesteJumpState(_player),
            _ => throw new LogicException()
        };
}