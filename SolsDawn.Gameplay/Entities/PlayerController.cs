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
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, idleState));
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
                //IntendFire();
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
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, idleState));
            return;
        }
        
        if (_player.State is IdleState &&
                 Input.Move != Vector2.Zero)
        {
            var moveState = new MoveState(_player);
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, moveState));
            return;
        }
    }

    private void IntendTeleport()
    {
        var teleportState = new TeleportState(_player);
        IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, teleportState));
    }

    private void IntendBlade()
    {
        if (!_player.Board.BladeCharged)
            return;
        _player.Board.LastBladeUsage = TotalSeconds;

        var screenPosition = Input.Blade.ScreenPosition;
        var lookPosition = Camera.ScreenToWorld(screenPosition);
        
        var bladeState = new BladeState(_player, lookPosition);
        IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, bladeState));
    }

    /*private void IntendFire()
    {
        if (!_player.FireCharged)
            return;
        _player.LastFireUsage = Time.TotalGameTime.TotalSeconds;

        var screenPosition = _input.Fire.ScreenPosition;
        var lookPosition = Game.Camera.ScreenToWorld(screenPosition);
        var direction = lookPosition - _player.GameObject.Transform.Position;
        direction.Normalize();
        var fireEnd = _player.GameObject.Transform.Position + direction * _player.Stats.FireDistance;

        var vertices = PolygonTools.CreateRectangle(
            _player.Stats.FireDistance / 2f,
            _player.Stats.FireWidth / 2f);
        
        var shape = new PolygonShape(vertices, 1f);
        var targets = new List<GameObject>();
        var rotation = (float)Math.Atan2(direction.Y, direction.X);
        Query.Overlap(
            shape, 
            (_player.GameObject.Transform.Position + fireEnd) / 2, 
            rotation,
            Collision.Enemy, 
            targets,
            DebugCategory.Attack);

        var fireState = new Player.FireExecuteState(_player, lookPosition, targets);
        IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, fireState));
    }*/
}