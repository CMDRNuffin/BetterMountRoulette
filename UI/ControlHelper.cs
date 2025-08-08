namespace BetterMountRoulette.UI;

using Dalamud.Interface.Utility;

using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;

internal static class ControlHelper
{
    /// <summary>
    /// Works for disabled elements. Use <see cref="Tooltip(bool, ReadOnlySpan{byte})"/> if that is not desired.
    /// </summary>
    /// <param name="text">The text of the tooltip.</param>
    public static void Tooltip(ReadOnlySpan<byte> text)
    {
        Tooltip(true, text);
    }

    public static void Tooltip(bool allowDisabled, ReadOnlySpan<byte> text)
    {
        if (BeginTooltip(allowDisabled))
        {
            ImGui.Text(text);
            ImGui.EndTooltip();
        }
    }

    public static bool BeginTooltip(bool allowDisabled = true)
    {
        ImGuiHoveredFlags flags = allowDisabled
            ? ImGuiHoveredFlags.AllowWhenDisabled
            : ImGuiHoveredFlags.None;
        if (ImGui.IsItemHovered(flags))
        {
            ImGui.BeginTooltip();
            return true;
        }

        return false;
    }

    public static void SelectItem<T>(IEnumerable<T> items, Func<T, string> nameSelector, ref string selectedName, ReadOnlySpan<byte> label, float? width = null)
    {
        if (width is float w)
        {
            ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * w);
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

    public static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3)
    {
        byte[] res = new byte[part1.Length + part2.Length + part3.Length];
        part1.CopyTo(res.AsSpan());
        part2.CopyTo(res.AsSpan(part1.Length));
        part3.CopyTo(res.AsSpan(part1.Length + part2.Length));
        return res;
    }
}
