using ImGuiNET;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Configs;

namespace SolsDawn.Gameplay;

public static class Debug
{
    private static bool _showLibraryBrowser = true;

    public static void Draw()
    {
        ImGui.SetNextWindowPos(Vector2.Zero.ToNumerics(), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 760), ImGuiCond.FirstUseEver);
        ImGui.Begin("Player Configuration & Inspector");

        ImGui.Text("Debug Controls");
        ImGui.Text($"Position: ({G.Player.Transform.Position.X:F2}, {G.Player.Transform.Position.Y:F2})");
        ImGui.Text($"Active State: {G.Player.State?.GetType().Name ?? "None"}");

        var zoom = Camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 100f))
            Camera.Zoom = zoom;

        var pSpecs = G.Player.Board.Specs;

        if (ImGui.Button("Save Config JSON"))
        {
            PlayerConfigs.Save(pSpecs);
        }
        ImGui.SameLine();
        if (ImGui.Button("Reload Config JSON"))
        {
            G.Player.Board.Specs = PlayerConfigs.Load();
            pSpecs = G.Player.Board.Specs;
        }
        ImGui.SameLine();
        ImGui.Checkbox("Library Browser", ref _showLibraryBrowser);

        ImGui.Separator();

        // Draws all State CategorySlots (Idle, Run, Jump, Fall) and their nested MotionSolvers
        AutoInspector.Draw(pSpecs);

        ImGui.End();

        AutoInspector.DrawLibraryBrowser(ref _showLibraryBrowser);
    }
}