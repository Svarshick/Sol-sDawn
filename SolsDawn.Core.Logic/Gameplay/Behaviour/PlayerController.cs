using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class PlayerController
{
    private readonly Player _player;
    private readonly Input _input;
    
    public PlayerController(Player player, Input input)
    {
        _player = player;
        _input = input;
    }

    public void Update()
    {
        if (_player.State is Player.TeleportState &&
            _input.Teleport.State == Input.TeleportState.Released)
        {
            var idleState = new Player.IdleState(_player);
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, idleState));
            return;   
        }
        
        if (_player.State is Player.IdleState or Player.MoveState)
        {
            if (_input.Blade.IsPressed)
            {
                IntendBlade();
                return;
            }

            if (_input.Fire.IsPressed)
            {
                //IntendFire();
                return;
            }

            if (_input.Teleport.State == Input.TeleportState.Started)
            {
                IntendTeleport();
                return;
            }
        }

        if (_player.State is Player.MoveState &&
            _input.Move == Vector2.Zero)
        {
            var idleState = new Player.IdleState(_player);
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, idleState));
            return;
        }
        
        if (_player.State is Player.IdleState &&
                 _input.Move != Vector2.Zero)
        {
            var moveState = new Player.MoveState(_player, _input);
            IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, moveState));
            return;
        }
    }

    private void IntendTeleport()
    {
        var teleportState = new Player.TeleportState(_player, _input);
        IntentionsPool.AddIntention(new EnterStateIntention(_player.GameObject, teleportState));
    }

    private void IntendBlade()
    {
        if (!_player.BladeCharged)
            return;
        _player.LastBladeUsage = Time.TotalGameTime.TotalSeconds;

        var screenPosition = _input.Blade.ScreenPosition;
        var lookPosition = SolsDawn.Camera.ScreenToWorld(screenPosition);
        
        var bladeState = new Player.BladeState(_player, lookPosition);
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