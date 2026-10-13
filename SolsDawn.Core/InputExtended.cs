//Copied from MonoGame.Extended
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SolsDawn.Core;

#nullable disable

[Flags]
public enum MouseButton
{
  None = 0,
  Left = 1,
  Middle = 2,
  Right = 4,
  XButton1 = 8,
  XButton2 = 16, // 0x00000010
}

/// <summary>Represents mouse input.</summary>
/// <remarks>
/// This si an extended version of the default <see cref="T:Microsoft.Xna.Framework.Input.Mouse" /> class which offers
/// internal tracking of both the previous and current state of mouse input.
/// </remarks>
public static class MouseExtended
{
  private static MouseState _currentMouseState;
  private static MouseState _previousMouseState;

  /// <summary>Gets the state of mouse input.</summary>
  /// <returns>
  /// A <see cref="T:MonoGame.Extended.Input.MouseStateExtended" /> value that represents the state of mouse input.
  /// </returns>
  public static MouseStateExtended GetState()
  {
    return new MouseStateExtended(MouseExtended._currentMouseState, MouseExtended._previousMouseState);
  }

  /// <summary>
  /// Updates the <see cref="T:MonoGame.Extended.Input.MouseExtended" />.
  /// </summary>
  /// <remarks>
  /// This internally will overwrite the source data for the previous state with the current state, then get the
  /// current state from the mouse input.  This should only be called once per update cycle.  Calling it more than
  /// once per update cycle can result in the cached previous state being overwritten with invalid data.
  /// </remarks>
  public static void Update()
  {
    MouseExtended._previousMouseState = MouseExtended._currentMouseState;
    MouseExtended._currentMouseState = Mouse.GetState();
  }

  /// <summary>
  /// Sets the position of the mouse cursor to the specified coordinates relative to the game window.
  /// </summary>
  /// <param name="x">The x-coordinate position.</param>
  /// <param name="y">The y-coordinate position.</param>
  public static void SetPosition(int x, int y) => Mouse.SetPosition(x, y);

  /// <summary>
  /// Sets the position of the mouse cursor to the specified coordinate relative to the game window.
  /// </summary>
  /// <param name="point">A <see cref="T:Microsoft.Xna.Framework.Point" /> value that represents the x- and y-coordinate positions.</param>
  public static void SetPosition(Point point) => Mouse.SetPosition(point.X, point.Y);

  /// <summary>Sets the cursor of the mouse.</summary>
  /// <param name="cursor">The cursor to use.</param>
  public static void SetCursor(MouseCursor cursor) => Mouse.SetCursor(cursor);

  /// <summary>Gets or Sets the window handle of the mouse.</summary>
  public static IntPtr WindowHandle
  {
    get => Mouse.WindowHandle;
    set => Mouse.WindowHandle = value;
  }
}

/// <summary>Represents the state of mouse input.</summary>
/// <remarks>
/// This is an extended version of the base <see cref="T:Microsoft.Xna.Framework.Input.MouseState" /> struct
/// that provides utility for checking state differences between the previous and current state.
/// </remarks>
/// <summary>
/// Initializes a new instance of the <see cref="T:MonoGame.Extended.Input.MouseStateExtended" /> value.
/// </summary>
/// <param name="currentMouseState">The state of mouse input during the current update cycle.</param>
/// <param name="previousMouseState">THe state of mouse input during hte previous update cycle.</param>
public struct MouseStateExtended(MouseState currentMouseState, MouseState previousMouseState)
{
  private readonly MouseState _currentMouseState = currentMouseState;
  private readonly MouseState _previousMouseState = previousMouseState;

  /// <summary>
  /// Gets the current x-coordinate position of the mouse cursor relative to the game window.
  /// </summary>
  public int X => this._currentMouseState.X;

  /// <summary>
  /// Gets the current y-coordinate position of the mouse cursor relative to the game window.
  /// </summary>
  public int Y => this._currentMouseState.Y;

  /// <summary>
  /// Gets the current xy-coordinate position of the mouse cursor relative to the game window.
  /// </summary>
  public Point Position
  {
    get
    {
      MouseState currentMouseState = this._currentMouseState;
      int x = currentMouseState.X;
      currentMouseState = this._currentMouseState;
      int y = currentMouseState.Y;
      return new Point(x, y);
    }
  }

  /// <summary>
  /// Gets a value that indicates whether the position of the mouse cursor changes between the previous and current
  /// states.
  /// </summary>
  public bool PositionChanged
  {
    get
    {
      MouseState mouseState1 = this._currentMouseState;
      int x1 = mouseState1.X;
      mouseState1 = this._previousMouseState;
      int x2 = mouseState1.X;
      if (x1 != x2)
        return true;
      MouseState mouseState2 = this._currentMouseState;
      int y1 = mouseState2.Y;
      mouseState2 = this._previousMouseState;
      int y2 = mouseState2.Y;
      return y1 != y2;
    }
  }

  /// <summary>
  /// Gets the difference in the x-coordinate position change of the mouse between the previous and current state.
  /// </summary>
  public int DeltaX
  {
    get
    {
      MouseState mouseState = this._previousMouseState;
      int x1 = mouseState.X;
      mouseState = this._currentMouseState;
      int x2 = mouseState.X;
      return x1 - x2;
    }
  }

  /// <summary>
  /// Gets the difference in the y-coordinate position change of the mouse between the previous and current state.
  /// </summary>
  public int DeltaY
  {
    get
    {
      MouseState mouseState = this._previousMouseState;
      int y1 = mouseState.Y;
      mouseState = this._currentMouseState;
      int y2 = mouseState.Y;
      return y1 - y2;
    }
  }

  /// <summary>
  /// Gets the difference in the xy-coordinate position change of the mouse between the previous and curren state.
  /// </summary>
  public Point DeltaPosition => new Point(this.DeltaX, this.DeltaY);

  /// <summary>Gets the current value of the mouse scroll wheel.</summary>
  public int ScrollWheelValue => this._currentMouseState.ScrollWheelValue;

  /// <summary>
  /// Gets the difference in the mouse scroll wheel value between the previous and current state.
  /// </summary>
  public int DeltaScrollWheelValue
  {
    get
    {
      MouseState mouseState = this._previousMouseState;
      int scrollWheelValue1 = mouseState.ScrollWheelValue;
      mouseState = this._currentMouseState;
      int scrollWheelValue2 = mouseState.ScrollWheelValue;
      return scrollWheelValue1 - scrollWheelValue2;
    }
  }

  /// <summary>Gets the current state of the mouse left button.</summary>
  public ButtonState LeftButton => this._currentMouseState.LeftButton;

  /// <summary>Gets the current state of the mouse middle button.</summary>
  public ButtonState MiddleButton => this._currentMouseState.MiddleButton;

  /// <summary>Gets the current state of the mouse right button.</summary>
  public ButtonState RightButton => this._currentMouseState.RightButton;

  /// <summary>Gets the current state of the first mouse extra button.</summary>
  public ButtonState XButton1 => this._currentMouseState.XButton1;

  /// <summary>
  /// Gets the current state of the second mouse extra button.
  /// </summary>
  public ButtonState XButton2 => this._currentMouseState.XButton2;

  /// <summary>
  /// Returns a value that indicates whether the specified mouse button is down during the current state.
  /// </summary>
  /// <param name="button">The mouse button to check.</param>
  /// <returns>
  /// <see langword="true" /> if the mouse button is down during the current state; otherwise, <see langword="false" />.
  /// </returns>
  public bool IsButtonDown(MouseButton button)
  {
    switch (button)
    {
      case MouseButton.Left:
        return this.IsPressed((Func<MouseState, ButtonState>) (m => m.LeftButton));
      case MouseButton.Middle:
        return this.IsPressed((Func<MouseState, ButtonState>) (m => m.MiddleButton));
      case MouseButton.Right:
        return this.IsPressed((Func<MouseState, ButtonState>) (m => m.RightButton));
      case MouseButton.XButton1:
        return this.IsPressed((Func<MouseState, ButtonState>) (m => m.XButton1));
      case MouseButton.XButton2:
        return this.IsPressed((Func<MouseState, ButtonState>) (m => m.XButton2));
      default:
        return false;
    }
  }

  /// <summary>
  /// Returns a value that indicates whether the specified mouse button is up during the current state.
  /// </summary>
  /// <param name="button">The mouse button to check.</param>
  /// <returns>
  /// <see langword="true" /> if the mouse button is up during the current state; otherwise, <see langword="false" />.
  /// </returns>
  public bool IsButtonUp(MouseButton button)
  {
    switch (button)
    {
      case MouseButton.Left:
        return this.IsReleased((Func<MouseState, ButtonState>) (m => m.LeftButton));
      case MouseButton.Middle:
        return this.IsReleased((Func<MouseState, ButtonState>) (m => m.MiddleButton));
      case MouseButton.Right:
        return this.IsReleased((Func<MouseState, ButtonState>) (m => m.RightButton));
      case MouseButton.XButton1:
        return this.IsReleased((Func<MouseState, ButtonState>) (m => m.XButton1));
      case MouseButton.XButton2:
        return this.IsReleased((Func<MouseState, ButtonState>) (m => m.XButton2));
      default:
        return false;
    }
  }

  /// <summary>
  /// Returns whether the specified mouse button was up during the previous, but is now down.
  /// </summary>
  /// <param name="button">The mouse button to check.</param>
  /// <returns>
  /// <see langword="true" /> if the mouse button was up pressed this state-change; otherwise, <see langword="false" />.
  /// </returns>
  public readonly bool WasButtonPressed(MouseButton button)
  {
    bool flag;
    switch (button)
    {
      case MouseButton.Left:
        flag = this.WasJustPressed((Func<MouseState, ButtonState>) (m => m.LeftButton));
        break;
      case MouseButton.Middle:
        flag = this.WasJustPressed((Func<MouseState, ButtonState>) (m => m.MiddleButton));
        break;
      case MouseButton.Right:
        flag = this.WasJustPressed((Func<MouseState, ButtonState>) (m => m.RightButton));
        break;
      case MouseButton.XButton1:
        flag = this.WasJustPressed((Func<MouseState, ButtonState>) (m => m.XButton1));
        break;
      case MouseButton.XButton2:
        flag = this.WasJustPressed((Func<MouseState, ButtonState>) (m => m.XButton2));
        break;
      default:
        flag = false;
        break;
    }
    return flag;
  }

  /// <summary>
  /// Returns whether the specified mouse button was down during the previous state, but is now up.
  /// </summary>
  /// <param name="button">The mouse button to check.</param>
  /// <returns>
  /// <see langword="true" /> if the mouse button was released this state-change; otherwise, <see langword="false" />.
  /// </returns>
  public readonly bool WasButtonReleased(MouseButton button)
  {
    bool flag;
    switch (button)
    {
      case MouseButton.Left:
        flag = this.WasJustReleased((Func<MouseState, ButtonState>) (m => m.LeftButton));
        break;
      case MouseButton.Middle:
        flag = this.WasJustReleased((Func<MouseState, ButtonState>) (m => m.MiddleButton));
        break;
      case MouseButton.Right:
        flag = this.WasJustReleased((Func<MouseState, ButtonState>) (m => m.RightButton));
        break;
      case MouseButton.XButton1:
        flag = this.WasJustReleased((Func<MouseState, ButtonState>) (m => m.XButton1));
        break;
      case MouseButton.XButton2:
        flag = this.WasJustReleased((Func<MouseState, ButtonState>) (m => m.XButton2));
        break;
      default:
        flag = false;
        break;
    }
    return flag;
  }

  private readonly bool IsPressed(Func<MouseState, ButtonState> button)
  {
    return button(this._currentMouseState) == ButtonState.Pressed;
  }

  private readonly bool IsReleased(Func<MouseState, ButtonState> button)
  {
    return button(this._currentMouseState) == ButtonState.Released;
  }

  private readonly bool WasJustPressed(Func<MouseState, ButtonState> button)
  {
    return button(this._previousMouseState) == ButtonState.Released && button(this._currentMouseState) == ButtonState.Pressed;
  }

  private readonly bool WasJustReleased(Func<MouseState, ButtonState> button)
  {
    return button(this._previousMouseState) == ButtonState.Pressed && button(this._currentMouseState) == ButtonState.Released;
  }
}

/// <summary>Represents keyboard input.</summary>
/// <remarks>
/// This is an extended version of the default <see cref="T:Microsoft.Xna.Framework.Input.Keyboard" /> class
/// which offers internal tracking of both the previous and current state of keyboard input.
/// </remarks>
public static class KeyboardExtended
{
  private static KeyboardState _currentKeyboardState;
  private static KeyboardState _previousKeyboardState;

  /// <summary>Gets the state of keyboard input.</summary>
  /// <returns>
  /// A <see cref="T:MonoGame.Extended.Input.KeyboardStateExtended" /> value that represents the state of keyboard input.
  /// </returns>
  public static KeyboardStateExtended GetState()
  {
    return new KeyboardStateExtended(KeyboardExtended._currentKeyboardState, KeyboardExtended._previousKeyboardState);
  }

  /// <summary>
  /// Updates the <see cref="T:MonoGame.Extended.Input.KeyboardExtended" />
  /// </summary>
  /// <remarks>
  /// This internally will overwrite the source data for the previous state with the current state, then get the
  /// current state from the keyboard input.  This should only be called once per update cycle.  Calling it more
  /// than once per update cycle can result in the cached previous state being overwritten with invalid data.
  /// </remarks>
  public static void Update()
  {
    KeyboardExtended._previousKeyboardState = KeyboardExtended._currentKeyboardState;
    KeyboardExtended._currentKeyboardState = Keyboard.GetState();
  }
}

/// <summary>Represents the state of keyboard input</summary>
/// <remarks>
/// This is an extended version of the base <see cref="T:Microsoft.Xna.Framework.Input.KeyboardState" /> struct
/// that provides utility for checking the state differences between the previous and current state.
/// </remarks>
/// <summary>
/// Initializes a new instance of the <see cref="T:MonoGame.Extended.Input.KeyboardStateExtended" /> value.
/// </summary>
/// <param name="currentKeyboardState">The state of keyboard input during the current update cycle.</param>
/// <param name="previousKeyboardState">The state of keyboard input during the previous update cycle.</param>
public struct KeyboardStateExtended(
  KeyboardState currentKeyboardState,
  KeyboardState previousKeyboardState)
{
  private KeyboardState _currentKeyboardState = currentKeyboardState;
  private KeyboardState _previousKeyboardState = previousKeyboardState;

  /// <summary>
  /// Gets a value that indicates whether the caps lock key is down during the current state.
  /// </summary>
  public bool CapsLock => this._currentKeyboardState.CapsLock;

  /// <summary>
  /// Gets a value that indicates whether the num lock key is down during the current state.
  /// </summary>
  public bool NumLock => this._currentKeyboardState.NumLock;

  /// <summary>
  /// Returns a value that indicates whether either the left or right shift key is down during the current state.
  /// </summary>
  /// <returns>
  /// <see langword="true" /> if either the left or right shift key is down during the current state; otherwise,
  /// <see langword="false" />.
  /// </returns>
  public bool IsShiftDown()
  {
    return this._currentKeyboardState.IsKeyDown(Keys.LeftShift) || this._currentKeyboardState.IsKeyDown(Keys.RightShift);
  }

  /// <summary>
  /// Returns a value that indicates whether either the left or right control key is down during the current
  /// state.
  /// </summary>
  /// <returns>
  /// <see langword="true" /> if either the left or right control key is down during the current state; otherwise,
  /// <see langword="false" />.
  /// </returns>
  public bool IsControlDown()
  {
    return this._currentKeyboardState.IsKeyDown(Keys.LeftControl) || this._currentKeyboardState.IsKeyDown(Keys.RightControl);
  }

  /// <summary>
  /// Returns a value that indicates whether either the left or righ talt key is currently pressed down.
  /// </summary>
  /// <returns></returns>
  public bool IsAltDown()
  {
    return this._currentKeyboardState.IsKeyDown(Keys.LeftAlt) || this._currentKeyboardState.IsKeyDown(Keys.RightAlt);
  }

  /// <summary>
  /// Returns a value that indicates if the specified key is down during the current state.
  /// </summary>
  /// <param name="key">The key to check.</param>
  /// <returns>
  /// <see langword="true" /> if the key is down during the current state; otherwise, <see langword="false" />.
  /// </returns>
  public bool IsKeyDown(Keys key) => this._currentKeyboardState.IsKeyDown(key);

  /// <summary>
  /// Returns a value that indicates if the specified key is up during the current state.
  /// </summary>
  /// <param name="key">The key to check.</param>
  /// <returns>
  /// <see langword="true" /> if the key is up during the current state; otherwise, <see langword="false" />.
  /// </returns>
  public bool IsKeyUp(Keys key) => this._currentKeyboardState.IsKeyUp(key);

  /// <summary>
  /// Returns the total number of keys down during the current state.
  /// </summary>
  public int GetPressedKeyCount => this._currentKeyboardState.GetPressedKeyCount();

  /// <summary>
  /// Returns an array of all keys that are down during the current state.
  /// </summary>
  /// <returns>
  /// An array of <see cref="T:Microsoft.Xna.Framework.Input.Keys" /> values that represent each key that is down during the current state.
  /// </returns>
  public Keys[] GetPressedKeys() => this._currentKeyboardState.GetPressedKeys();

  /// <summary>
  /// Fills an existing array with the keys pressed during the current state.
  /// </summary>
  /// <param name="keys">An existing array to fill with the pressed keys.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// Thrown if the <paramref name="keys" /> parameter is <see langword="null" />.
  /// </exception>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  /// Thrown if the array provided by the <paramref name="keys" /> parameter is not large enough to fit all
  /// pressed keys.  Use <see cref="P:MonoGame.Extended.Input.KeyboardStateExtended.GetPressedKeyCount" /> to determine the total number of elements.
  /// </exception>
  public void GetPressedKeys(Keys[] keys) => this._currentKeyboardState.GetPressedKeys(keys);

  /// <summary>
  /// Returns whether the given key was down during previous state, but is now up.
  /// </summary>
  /// <param name="key">The key to check.</param>
  /// <returns>
  /// <see langword="true" /> if the key was released this state-change, otherwise <see langword="false" />.
  /// </returns>
  public readonly bool WasKeyReleased(Keys key)
  {
    return this._previousKeyboardState.IsKeyDown(key) && this._currentKeyboardState.IsKeyUp(key);
  }

  /// <summary>
  /// Returns whether the given key was up during previous state, but is now down.
  /// </summary>
  /// <param name="key">The key to check.</param>
  /// <returns>
  /// <see langword="true" /> if the key was pressed this state-change, otherwise <see langword="false" />.
  /// </returns>
  public readonly bool WasKeyPressed(Keys key)
  {
    return this._previousKeyboardState.IsKeyUp(key) && this._currentKeyboardState.IsKeyDown(key);
  }

  /// <summary>
  /// Returns whether any key was pressed down on the previous state.
  /// </summary>
  /// <returns>
  /// <see langword="true" /> if any key was pressed during the previous state; otherwise, <see langword="false" />.
  /// </returns>
  public bool WasAnyKeyJustDown() => this._previousKeyboardState.GetPressedKeyCount() > 0;
}