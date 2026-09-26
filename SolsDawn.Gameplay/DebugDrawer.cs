using ImGuiNET;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay;

public static class DebugDrawer
{
    private static Vector2 _vec;
    private static int _int;
   
    public static void Draw()
    {
        ImGui.SetNextWindowPos(Vector2.Zero.ToNumerics(), ImGuiCond.Always);
        ImGui.Begin("Demo Window",
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav);

        ImGui.Text("Debug Controls");
        ImGui.Separator();

        var pSpecs = G.Player.Board.Specs;
        AutoInspector.Draw(pSpecs);
        
        ImGui.End();
    }
}