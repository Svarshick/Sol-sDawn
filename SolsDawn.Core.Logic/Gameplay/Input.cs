using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace SolsDawn.Core.Logic.Gameplay;


public class Input : IUpdatable
{
    public Vector2 Move { get; private set; } 
    public bool Jump { get; private set; }
    
    public enum TeleportState { None, Started, Updated, Released }
    public (Vector2 ScreenPosition, double ElapsedTime, TeleportState State) Teleport { get; private set; }
    public (Vector2 ScreenPosition, bool IsPressed) Fire { get; private set; }
    public (Vector2 ScreenPosition, bool IsPressed) Blade { get; private set; }
    
    public event Action<Vector2>? OnMove; // (Direction)
    public event Action? OnJump; // void
    
    public event Action<Vector2>? OnTeleportStarted; // (ScreenPoint)
    public event Action<Vector2, double>? OnTeleportUpdated; // (ScreenPoint, ElapsedTime)
    public event Action<Vector2, double>? OnTeleportReleased; // (ScreenPoint, ElapsedTime)
    
    public event Action<Vector2>? OnFire; // (ScreenPosition)
    public event Action<Vector2>? OnBlade; // (ScreenPosition)

    public void Update()
    {
        KeyboardExtended.Update();
        MouseExtended.Update();
        var keyboardState = KeyboardExtended.GetState();
        var mouseState = MouseExtended.GetState();
        UpdateMovement(mouseState, keyboardState);
        UpdateTeleport(mouseState, keyboardState);
        UpdateAttacks(mouseState, keyboardState);
    }
    
    public void LateUpdate() { }

    private void UpdateMovement(
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState)
    {
        var moveDirection = Vector2.Zero;
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

        Move = moveDirection;
        OnMove?.Invoke(moveDirection);
        
        if (keyboardState.WasKeyPressed(Keys.Space))
        {
            Jump = true;
            OnJump?.Invoke();
        }
        else
        {
            Jump = false;
        }
    }

    private void UpdateTeleport(
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState)
    {
        var tpDown = keyboardState.IsKeyDown(Keys.T); 
        var mousePosition = mouseState.Position.ToVector2();
        var elapsedTime = Teleport.ElapsedTime + Time.ElapsedGameTime.TotalSeconds;
        
        switch (Teleport.State)
        {
            case TeleportState.None:
                if (tpDown)
                {
                    Teleport = (mousePosition, 0, TeleportState.Started);
                    OnTeleportStarted?.Invoke(mousePosition);
                }
                break;
            
            case TeleportState.Started:
            case TeleportState.Updated:
                if (tpDown)
                {
                    Teleport = (mousePosition, elapsedTime, TeleportState.Updated);
                    OnTeleportUpdated?.Invoke(mousePosition, elapsedTime);
                }
                else
                {
                    Teleport = (mousePosition, elapsedTime, TeleportState.Released);
                    OnTeleportReleased?.Invoke(mousePosition, elapsedTime);
                }
                break;
            
            case TeleportState.Released:
                Teleport = (Vector2.Zero, 0, TeleportState.None);
                break;
        }
    }

    private void UpdateAttacks(
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState
    )
    {
        var mousePosition = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        if (mouseState.WasButtonPressed(MouseButton.Left))
        {
            Blade = (mousePosition, true);
            OnBlade?.Invoke(mousePosition);
        }
        else
        {
            Blade = (Vector2.Zero, false);
        }

        if (mouseState.WasButtonPressed(MouseButton.Right))
        {
            Fire = (mousePosition, true);
            OnFire?.Invoke(mousePosition);
        }
        else
        {
            Fire = (Vector2.Zero, false);
        }
    }
}