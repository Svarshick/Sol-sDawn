using System;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using NumVector2 = System.Numerics.Vector2;

namespace SolsDawn.Engine;

internal static class SaveSystem
{
    private const string PromptTitle = "Unsaved changes";

    private static bool _exitConfirmed;
    private static bool _openPromptPending;

    public static bool ExitPromptOpen { get; private set; }

    public static bool HasUnsavedChanges => Library.HasUnsavedChanges || SpeedGraphs.HasUnsavedChanges;

    public static bool Save()
    {
        var libraryOk = Library.Save();
        var graphsOk = SpeedGraphs.Save();
        if (libraryOk && graphsOk)
            Log.Logic.Info("Saved.");
        return libraryOk && graphsOk;
    }

    public static void Update()
    {
        var keyboard = KeyboardExtended.GetState();
        if (keyboard.IsControlDown() && keyboard.WasKeyPressed(Keys.S))
            Save();
    }

    public static bool CancelExit()
    {
        if (_exitConfirmed || !HasUnsavedChanges)
            return false;

        ExitPromptOpen = true;
        _openPromptPending = true;
        return true;
    }

    public static void DrawExitPrompt(Action exit)
    {
        if (!ExitPromptOpen)
            return;

        if (_openPromptPending)
        {
            ImGui.OpenPopup(PromptTitle);
            _openPromptPending = false;
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new NumVector2(0.5f, 0.5f));
        if (!ImGui.BeginPopupModal(PromptTitle, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ExitPromptOpen = false;
            return;
        }

        ImGui.TextUnformatted("Unsaved changes. Save?");
        ImGui.Spacing();

        if (ImGui.Button("Yes", new NumVector2(80, 0)) && Save())
            Close(exit);

        ImGui.SameLine();
        if (ImGui.Button("No", new NumVector2(80, 0)))
            Close(exit);

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new NumVector2(80, 0)))
        {
            ExitPromptOpen = false;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static void Close(Action exit)
    {
        _exitConfirmed = true;
        ExitPromptOpen = false;
        ImGui.CloseCurrentPopup();
        exit();
    }
}
