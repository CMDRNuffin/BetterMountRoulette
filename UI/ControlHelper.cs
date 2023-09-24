namespace BetterMountRoulette.UI;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class ControlHelper
{
    /// <summary>
    /// Works for disabled elements. Use <see cref="Tooltip(bool, string[])"/> if that is not desired.
    /// </summary>
    /// <param name="lines">The individual lines of the tooltip.</param>
    public static void Tooltip(params string[] lines)
    {
        Tooltip(true, lines);
    }

    public static void Tooltip(bool allowDisabled, params string[] lines)
    {
        if (lines.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        ImGuiHoveredFlags flags = allowDisabled
            ? ImGuiHoveredFlags.AllowWhenDisabled
            : ImGuiHoveredFlags.None;
        if (ImGui.IsItemHovered(flags))
        {
            ImGui.BeginTooltip();
            foreach(string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.Text(line);
                }
            }

            ImGui.EndTooltip();
        }
    }

    public static int? Buttons(params string[] labels)
    {
        int? result = null;
        for (int i = 0; i < labels.Length; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
            }

            if (ImGui.Button(labels[i]))
            {
                result = i;
            }
        }

        return result;
    }

    public static void SelectItem<T>(IEnumerable<T> items, Func<T, string> nameSelector, ref string selectedName, string label, float? width = null)
    {
        if (width is float w)
        {
            ImGui.SetNextItemWidth(w);
        }

        if (ImGui.BeginCombo(label, selectedName))
        {
            foreach (T item in items)
            {
                string name = nameSelector(item);
                if (ImGui.Selectable(name, name == selectedName))
                {
                    selectedName = name;
                }
            }

            ImGui.EndCombo();
        }
    }

    public static bool Combo(string label, ref uint selectedIndex, string[] values)
    {
        int selected = (int)selectedIndex;
        ImGui.Text(label);
        ImGui.SameLine();
        bool res = ImGui.Combo($"##{label}", ref selected, values, values.Length);
        if (res)
        {
            selectedIndex = (uint)selected;
        }

        return res;
    }
}
