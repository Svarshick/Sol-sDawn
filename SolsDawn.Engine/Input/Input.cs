using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SolsDawn.Engine;


internal class Input : IUpdatable
{
    [Flags]
    public enum MoveKeys { None = 0, Right = 1, Left = 2, Up = 4, Down = 8 }
    public MoveKeys MoveHeld { get; private set; }
    public Vector2 Move { get; private set; }
    public bool Jump { get; private set; }
    
    public enum TeleportState { None, Started, Updated, Released }
    public (Vector2 ScreenPosition, double ElapsedTime, TeleportState State) Teleport { get; private set; }
    public (Vector2 ScreenPosition, bool IsPressed) Fire { get; private set; }
    public (Vector2 ScreenPosition, bool IsPressed) Blade { get; private set; }
    
    public event Action<Vector2>? OnMove;
    public event Action? OnJump;
    
    public event Action<Vector2>? OnTeleportStarted;
    public event Action<Vector2, double>? OnTeleportUpdated;
    public event Action<Vector2, double>? OnTeleportReleased;
    
    public event Action<Vector2>? OnFire;
    public event Action<Vector2>? OnBlade;

    public void Update()
    {
        KeyboardExtended.Update();
        MouseExtended.Update();
        
        var keyboardState = KeyboardExtended.GetState();
        var mouseState = MouseExtended.GetState();

        if (GameplayAPI.WantCaptureKeyboard)
        {
            keyboardState.CurrentKeyboardState = default;
        }

        if (GameplayAPI.WantCaptureMouse)
        {
            mouseState.CurrentMouseState = new MouseState(
                mouseState.PreviousMouseState.X, 
                mouseState.PreviousMouseState.Y, 
                mouseState.PreviousMouseState.ScrollWheelValue, 
                ButtonState.Released, 
                ButtonState.Released, 
                ButtonState.Released, 
                ButtonState.Released, 
                ButtonState.Released,
                mouseState.PreviousMouseState.HorizontalScrollWheelValue);
        }
        
        UpdateMovement(mouseState, keyboardState);
        UpdateTeleport(mouseState, keyboardState);
        UpdateAttacks(mouseState, keyboardState);
    }
    
    public void LateUpdate() { }

    private void UpdateMovement(
        MouseStateExtended mouseState,
        KeyboardStateExtended keyboardState)
    {
        var held = MoveKeys.None;
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            held |= MoveKeys.Right;
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            held |= MoveKeys.Left;
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            held |= MoveKeys.Up;
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            held |= MoveKeys.Down;

        MoveHeld = held;
        Move = Direction(held);
        OnMove?.Invoke(Move);
        
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

    public static Vector2 Direction(MoveKeys held)
    {
        var v = new Vector2(
            (held.HasFlag(MoveKeys.Right) ? 1 : 0) - (held.HasFlag(MoveKeys.Left) ? 1 : 0),
            (held.HasFlag(MoveKeys.Up) ? 1 : 0) - (held.HasFlag(MoveKeys.Down) ? 1 : 0));
        return v == Vector2.Zero ? v : Vector2.Normalize(v);
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