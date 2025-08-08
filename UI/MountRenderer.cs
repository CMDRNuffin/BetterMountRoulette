namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal sealed class MountRenderer(PluginServices services)
{
    private const int PAGE_SIZE = COLUMNS * ROWS;
    private const int COLUMNS = 5;
    private const int ROWS = 6;

    private readonly PluginServices _services = services;

    public void RenderPage(List<MountData> mounts, MountGroup group, int page)
    {
        int i = 0;
        foreach (MountData mount in mounts.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE))
        {
            if (i++ > 0)
            {
                ImGui.SameLine();
            }

            if (i >= COLUMNS)
            {
                i = 0;
            }

            bool enabled = group.IncludedMounts.Contains(mount.ID) == group.IncludedMeansActive;
            enabled = Render(mount, enabled);
            _ = enabled == group.IncludedMeansActive
                ? group.IncludedMounts.Add(mount.ID)
                : group.IncludedMounts.Remove(mount.ID);
        }
    }

    public static void Update(List<MountData> mounts, MountGroup group, bool selected, int? page)
    {
        IEnumerable<MountData> filteredMounts = mounts;
        if (page is not null)
        {
            filteredMounts = filteredMounts.Skip((page.Value - 1) * PAGE_SIZE).Take(PAGE_SIZE);
        }

        HashSet<uint> selectedMounts = group.IncludedMounts;

        Func<uint, bool> selectOperation = selected == group.IncludedMeansActive
            ? selectedMounts.Add
            : selectedMounts.Remove;

        foreach (MountData mount in filteredMounts)
        {
            _ = selectOperation(mount.ID);
        }
    }

    public static int GetPageCount(int mountCount)
    {
        return (mountCount / PAGE_SIZE) + (mountCount % PAGE_SIZE == 0 ? 0 : 1);
    }

    public bool Render(MountData mountData, bool enabled)
    {
        ImTextureID selectedUnselectedIcon = _services.TextureHelper.LoadUldTexture("readycheck");

        ImTextureID mountIcon = mountData.GetIcon();

        _ = ImGui.TableNextColumn();

        Vector2 originalPos = ImGui.GetCursorPos();

        const float BUTTON_SIZE = 60f;
        const float OVERLAY_SIZE = 24f;
        const float OVERLAY_OFFSET = 4f;
        var buttonSize = new Vector2(BUTTON_SIZE);
        var overlaySize = new Vector2(OVERLAY_SIZE);

        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

        if (ImGui.ImageButton(mountIcon, buttonSize, Vector2.Zero, Vector2.One, 0))
        {
            enabled ^= true;
        }

        ImGui.PopStyleColor(3);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(StringCache.Mounts[mountData.ID, mountData.Name.ExtractText]);
        }

        Vector2 finalPos = ImGui.GetCursorPos();

        // calculate overlay (top right corner) position
        Vector2 overlayPos = originalPos + new Vector2(buttonSize.X - overlaySize.X + OVERLAY_OFFSET, 0);
        ImGui.SetCursorPos(overlayPos);

        Vector2 offset = new(enabled ? 0.1f : 0.6f, 0.2f);
        Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
        ImGui.Image(selectedUnselectedIcon, overlaySize, offset, offset2);

        // put cursor back to where it was after rendering the button to prevent
        // messing up the table rendering
        ImGui.SetCursorPos(finalPos);

        return enabled;
    }
}
