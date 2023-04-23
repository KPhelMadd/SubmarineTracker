﻿using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SubmarineTracker.Data;
using System;
using System.Numerics;

namespace SubmarineTracker.Windows;

public partial class HelpyWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public HelpyWindow(Plugin plugin, Configuration configuration) : base("Helpy")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(680, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, "No Data, pls talk to the Voyage Control Panel -> Submersible Management.");
            return;
        }

        var buttonHeight = ImGui.CalcTextSize("XXX").Y + 10.0f;
        if (ImGui.BeginChild("SubContent", new Vector2(0, -(buttonHeight + (30.0f * ImGuiHelpers.GlobalScale)))))
        {
            if (ImGui.BeginTabBar("##helperTabBar"))
            {
                ProgressionTab(fcSub);

                StorageTab(fcSub);
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (ImGui.Button("Settings"))
                Plugin.DrawConfigUI();
        }
        ImGui.EndChild();
    }
}
