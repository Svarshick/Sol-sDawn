using ImGuiNET;
using SolsDawn.Core.Logic;

namespace SolsDawn.Gameplay;

public static class Debug
{
    private static bool _showLibraryBrowser = true;

    public static void Draw()
    {
        ImGui.SetNextWindowPos(Vector2.Zero.ToNumerics(), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 760), ImGuiCond.FirstUseEver);
        ImGui.Begin("Player Configuration & Inspector");

        ImGui.Text($"Position: ({G.Player.Transform.Position.X:F2}, {G.Player.Transform.Position.Y:F2})");
        ImGui.Text($"Active State: {G.Player.State?.GetType().Name ?? "None"}");

        var zoom = Camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 100f))
            Camera.Zoom = zoom;

        ImGui.Checkbox("Library Browser", ref _showLibraryBrowser);
        ImGui.Separator();

        // Root of config is now G.Player.Board -> renders CategorySlot "Specs [PlayerSpecs]" at root
        AutoInspector.Draw(G.Player.Board);

        ImGui.End();

        AutoInspector.DrawLibraryBrowser(ref _showLibraryBrowser);
    }
}