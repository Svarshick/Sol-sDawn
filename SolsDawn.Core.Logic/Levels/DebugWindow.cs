using ImGuiNET;
using Microsoft.Xna.Framework;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Levels;

public static class DebugWindow
{
    private static bool _showLibraryBrowser;

    public static void Draw()
    {
        ImGui.SetNextWindowPos(Vector2.Zero.ToNumerics(), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 760), ImGuiCond.FirstUseEver);
        ImGui.Begin("Player Configuration & Inspector");

        DrawLevels();

        if (G.Player is { IsDestroyed: false } player)
        {
            ImGui.Text($"Position: ({player.Transform.Position.X:F2}, {player.Transform.Position.Y:F2})");
            ImGui.Text($"Active State: {player.State?.GetType().Name ?? "None"}");
        }
        else
        {
            ImGui.Text("No player");
        }

        var zoom = Camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 100f))
            Camera.Zoom = zoom;

        ImGui.Checkbox("Library Browser", ref _showLibraryBrowser);
        ImGui.Separator();

        AutoInspector.Draw(Library.GetModelEntry(Library.RootGuid)!);

        ImGui.End();

        AutoInspector.DrawLibraryBrowser(ref _showLibraryBrowser);
    }

    private static void DrawLevels()
    {
        var current = LevelRunner.CurrentName ?? "";
        if (ImGui.BeginCombo("Level", current))
        {
            foreach (var level in LevelRunner.Levels)
            {
                if (ImGui.Selectable(level.Name, level.Name == current))
                    LevelRunner.Start(level.Start);
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Restart"))
            LevelRunner.Restart();
    }
}
