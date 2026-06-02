using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

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
            IntentionsPool.Add(new EnterStateIntention(_player.GameObject, idleState));
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
                IntendFire();
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
            IntentionsPool.Add(new EnterStateIntention(_player.GameObject, idleState));
            return;
        }
        
        if (_player.State is Player.IdleState &&
                 _input.Move != Vector2.Zero)
        {
            var moveState = new Player.MoveState(_player, _input);
            IntentionsPool.Add(new EnterStateIntention(_player.GameObject, moveState));
            return;
        }
    }

    private void IntendTeleport()
    {
        var teleportState = new Player.TeleportState(_player, _input);
        IntentionsPool.Add(new EnterStateIntention(_player.GameObject, teleportState));
    }

    private void IntendBlade()
    {
        if (!_player.BladeCharged)
            return;
        _player.LastBladeUsage = Time.TotalGameTime.TotalSeconds;

        var screenPosition = _input.Blade.ScreenPosition;
        var lookPosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var bladeDirection = lookPosition - _player.GameObject.Transform.Position;
        bladeDirection.Normalize();
        var bladeVertices = Helper.ArchVertices(
            _player.GameObject.Transform.Position,
            bladeDirection,
            _player.Stats.BladeDashDistance + _player.Stats.BladeAttackDistance,
            _player.Stats.Width,
            _player.Stats.BladeAttackEdgeAngle,
            _player.Stats.BladeAttackEdgeLength);

        var polygon = BoundingPolygon2D.CreateFromVertices(bladeVertices);
        var shape = new CollisionShape2D(polygon);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Enemy, targets);
        Collision.Overlap(shape, Collision.LayerName.Parry, targets);

        var bladeState = new Player.BladeExecuteState(_player, lookPosition, targets);
        IntentionsPool.Add(new EnterStateIntention(_player.GameObject, bladeState));
    }
    

    private void IntendFire()
    {
        if (!_player.FireCharged)
            return;
        _player.LastFireUsage = Time.TotalGameTime.TotalSeconds;

        var screenPosition = _input.Fire.ScreenPosition;
        var lookPosition = Game.ScreenLayout.Camera.ScreenToWorld(screenPosition);
        var direction = lookPosition - _player.GameObject.Transform.Position;
        direction.Normalize();
        var fireEnd = _player.GameObject.Transform.Position + direction * _player.Stats.FireDistance;

        var bounds = new OrientedBoundingBox2D(
            (_player.GameObject.Transform.Position + fireEnd) / 2,
            direction,
            direction.PerpendicularClockwise(),
            new Vector2(_player.Stats.FireDistance / 2, _player.Stats.FireWidth / 2));
        var shape = new CollisionShape2D(bounds);
        var targets = new List<GameObject>();
        Collision.Overlap(shape, Collision.LayerName.Enemy, targets);

        var fireState = new Player.FireExecuteState(_player, lookPosition, targets);
        IntentionsPool.Add(new EnterStateIntention(_player.GameObject, fireState));
    }
}