using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;

namespace SolsDawn.Core.Logic.Gameplay.Lua;

[MoonSharpUserData]
public class LuaInput(Input source)
{
    public Vector2 Move => source.Move;
    public LuaTeleportAction Teleport => new (source.Teleport.ScreenPosition, source.Teleport.ElapsedTime, source.Teleport.State);
    public LuaAction Blade => new (source.Blade.ScreenPosition, source.Blade.IsPressed);
    public LuaAction Fire => new (source.Fire.ScreenPosition, source.Fire.IsPressed);
}

public record LuaAction(Vector2 screenPosition, bool isPressed);
public record LuaTeleportAction(Vector2 screenPosition, double elapsedTime, Input.TeleportState state);