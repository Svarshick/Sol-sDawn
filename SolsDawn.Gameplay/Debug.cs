using System;
using System.Collections.Generic;
using ImGuiNET;
using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Entities;

namespace SolsDawn.Gameplay;

public static class Debug
{
    public static Type CurrentJumpHeuristic => JumpHeuristics[_jumpIndex].Type;
    private static int _jumpIndex = 0;
    private static readonly List<(string Name, Type Type)> JumpHeuristics = new()
    {
        (nameof(PrimitiveJumpState), typeof(PrimitiveJumpState)),
        (nameof(CelesteJumpState), typeof(CelesteJumpState)),
    };
    
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
        ImGui.Text($"Position: ({G.Player.Transform.Position.X:F2}, {G.Player.Transform.Position.Y:F2})");
        
        ImGui.Separator();

        if (ImGui.BeginCombo("Jump Heuristic", JumpHeuristics[_jumpIndex].Name))
        {
            for (int i = 0; i < JumpHeuristics.Count; i++)
            {
                bool isSelected = (_jumpIndex == i);
                if (ImGui.Selectable(JumpHeuristics[i].Name, isSelected))
                {
                    _jumpIndex = i;
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        var pSpecs = G.Player.Board.Specs;
        object jumpSpecs = CurrentJumpHeuristic.Name switch
        {
            nameof(PrimitiveJumpState) => pSpecs.PrimitiveJump,
            nameof(CelesteJumpState) => pSpecs.CelesteJump,
            _ => throw new LogicException()
        };
        AutoInspector.Draw(jumpSpecs);
        
        ImGui.Separator();

        ImGui.Text("Run");
        AutoInspector.Draw(pSpecs.Run);
        
        ImGui.Separator();
        
        ImGui.Text("Fall");
        AutoInspector.Draw(pSpecs.Fall);
        
        ImGui.End();
    }
}