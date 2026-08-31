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
        if (_player.State is null ||
            _player.State is TeleportState &&
            Input.Teleport.State == InputTeleportState.Released)
        {
            var idleState = new IdleState(_player);
            _player.Enter(idleState);
            return;   
        }
        
        if (_player.State is IdleState or MoveState)
        {
            if (Input.Blade.IsPressed)
            {
                IntendBlade();
                return;
            }

            if (Input.Fire.IsPressed)
            {
                IntendFire();
                return;
            }

            if (Input.Teleport.State == InputTeleportState.Started)
            {
                IntendTeleport();
                return;
            }
        }

        if (_player.State is MoveState &&
            Input.Move == Vector2.Zero)
        {
            var idleState = new IdleState(_player);
            _player.Enter(idleState);
            return;
        }
        
        if (_player.State is IdleState &&
                 Input.Move != Vector2.Zero)
        {
            var moveState = new MoveState(_player);
            _player.Enter(moveState);
            return;
        }
    }

    private void IntendTeleport()
    {
        if (!_player.Board.TeleportCharged)
            return;
        _player.Board.LastTeleportUsage = TotalSeconds;
        
        var teleportState = new TeleportState(_player);
        _player.Enter(teleportState);
    }

    private void IntendBlade()
    {
        if (!_player.Board.BladeCharged)
            return;
        _player.Board.LastBladeUsage = TotalSeconds;

        var screenPosition = Input.Blade.ScreenPosition;
        var lookPosition = Camera.ScreenToWorld(screenPosition);
        var bladeState = new BladeState(_player, lookPosition);
        _player.Enter(bladeState);
    }

    private void IntendFire()
    {
        if (!_player.Board.FireCharged)
            return;
        _player.Board.LastFireUsage = TotalSeconds;

        var screenPosition = Input.Fire.ScreenPosition;
        var lookPosition = Game.Camera.ScreenToWorld(screenPosition);
        var direction = lookPosition - _player.GameObject.Transform.Position;
        direction.Normalize();
        
        var fireState = new FireState(_player, direction);
        _player.Enter(fireState);
    }
}