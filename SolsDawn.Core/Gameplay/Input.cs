using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace SolsDawn.Core.Gameplay;

public class Input : IUpdatable
{
    public event Action<Vector2> Move; // (Direction)
    
    public event Action<Vector2> TeleportStarted; // (ScreenPoint)
    public event Action<Vector2, double> TeleportUpdated; // (ScreenPoint, ElapsedTime)
    public event Action<Vector2, double> TeleportReleased; // (ScreenPoint, ElapsedTime)
    private enum TeleportState { None, Started, Updated, Released }
    private TeleportState _teleportState;
    private double _teleportStartTime;
    
    public event Action<Vector2> Fire; // (ScreenPosition)
    public event Action<Vector2> Blade; // (ScreenPosition)

    public void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();
        MouseExtended.Update();
        var keyboardState = KeyboardExtended.GetState();
        var mouseState = MouseExtended.GetState();
        UpdateMove(gameTime, mouseState, keyboardState);
        UpdateTeleport(gameTime, mouseState, keyboardState);
        UpdateAttacks(gameTime, mouseState, keyboardState);
    }
    
    public void LateUpdate(GameTime gameTime) { }

    private void UpdateMove(
        GameTime gameTime, 
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState)
    {
        var moveDirection = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W) ||
            keyboardState.IsKeyDown(Keys.Up))
        {
            moveDirection.Y -= 1;
        }

        if (keyboardState.IsKeyDown(Keys.S) ||
            keyboardState.IsKeyDown(Keys.Down))
        {
            moveDirection.Y += 1;
        }

        if (keyboardState.IsKeyDown(Keys.D) ||
            keyboardState.IsKeyDown(Keys.Right))
        {
            moveDirection.X += 1;
        }

        if (keyboardState.IsKeyDown(Keys.A) ||
            keyboardState.IsKeyDown(Keys.Left))
        {
            moveDirection.X -= 1;
        }

        if (moveDirection != Vector2.Zero)
            moveDirection.Normalize();
        Move?.Invoke(moveDirection);
    }

    private void UpdateTeleport(
        GameTime gameTime, 
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState)
    {
        var spaceDown = keyboardState.IsKeyDown(Keys.Space); 
        var mousePosition = mouseState.Position.ToVector2();
        var elapsedTime = gameTime.TotalGameTime.TotalSeconds - _teleportStartTime;
        
        switch (_teleportState)
        {
            case TeleportState.None:
                if (spaceDown)
                {
                    _teleportState = TeleportState.Started;
                    _teleportStartTime = gameTime.TotalGameTime.TotalSeconds;
                    TeleportStarted?.Invoke(mousePosition);
                }
                break;
            
            case TeleportState.Started:
            case TeleportState.Updated:
                if (spaceDown)
                {
                    _teleportState = TeleportState.Updated;
                    TeleportUpdated?.Invoke(mousePosition, elapsedTime);
                }
                else
                {
                    _teleportState = TeleportState.Released;
                    TeleportReleased?.Invoke(mousePosition, elapsedTime);
                }
                break;
            
            case TeleportState.Released:
                _teleportState = TeleportState.None;
                _teleportStartTime = 0;
                break;
        }
    }

    private void UpdateAttacks(
        GameTime gameTime,
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState
    )
    {
        var mousePosition = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        if (mouseState.WasButtonPressed(MouseButton.Left))
        {
            Blade?.Invoke(mousePosition);
        }

        if (mouseState.WasButtonPressed(MouseButton.Right))
        {
            Fire?.Invoke(mousePosition);
        }
    }
}