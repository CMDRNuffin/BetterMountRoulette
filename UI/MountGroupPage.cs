namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;
using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class MountGroupPage
{
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly MountRenderer _mountRenderer;
    private string? _currentMountGroup;

    internal MountGroupPage(BetterMountRoulettePlugin plugin, Services services)
    {
        _plugin = plugin;
        _mountRenderer = new MountRenderer(services);
    }

    public void RenderPage(CharacterConfig characterConfig)
    {
        MountGroup mounts = SelectCurrentGroup(characterConfig);
        DrawMountGroup(mounts);
    }

    private void DrawMountGroup(MountGroup group)
    {
        if (group is null)
        {
            ImGui.Text("Group is null!");
            return;
        }

        RenderGroupSettings(group, out bool enableNewMounts);
        List<MountData> unlockedMounts = _plugin.MountRegistry.GetUnlockedMounts();
        UpdateMountSelectionData(group, unlockedMounts, enableNewMounts);

        int pages = MountRenderer.GetPageCount(_plugin.MountRegistry.UnlockedMountCount);
        if (pages == 0)
        {
            ImGui.Text("Please unlock at least one mount.");
        }
        else if (ImGui.BeginTabBar("mount_pages"))
        {
            for (int page = 1; page <= pages; page++)
            {
                if (ImGui.BeginTabItem($"{page}"))
                {
                    RenderMountListPage(page, group, unlockedMounts);
                    ImGui.EndTabItem();
                }
            }

            ImGui.SameLine();
            ImGui.EndTabBar();
        }
    }

    private void RenderMountListPage(int page, MountGroup group, List<MountData> unlockedMounts)
    {
        _mountRenderer.RenderPage(unlockedMounts, group, page);

        int currentPage = page;
        (bool Select, int? Page)? maybeInfo =
            ControlHelper.Buttons("Select all", "Unselect all", "Select page", "Unselect page") switch
            {
                0 => (true, default(int?)),
                1 => (false, default(int?)),
                2 => (true, page),
                3 => (false, page),
                _ => default((bool, int?)?),
            };

        if (maybeInfo is { } info)
        {
            string selectText = info.Select ? "select" : "unselect";
            string pageInfo = (info.Page, info.Select) switch
            {
                (null, true) => "currently unselected mounts",
                (null, false) => "currently selected mounts",
                _ => "mounts on the current page",
            };

            _plugin.WindowManager.ConfirmYesNo(
                "Are you sure?",
                $"Do you really want to {selectText} all {pageInfo}?",
                () => MountRenderer.Update(
                    _plugin.MountRegistry.GetUnlockedMounts(),
                    group,
                    info.Select,
                    info.Page));
        }
    }

    private static void RenderGroupSettings(MountGroup group, out bool enableNewMounts)
    {
        enableNewMounts = !group.IncludedMeansActive;
        _ = ImGui.Checkbox("Enable new mounts on unlock", ref enableNewMounts);

        bool forceMultiseatersInParty = group.ForceMultiseatersInParty;
        if (ImGui.Checkbox("Use only multiseater mounts in parties", ref forceMultiseatersInParty))
        {
            group.ForceMultiseatersInParty = forceMultiseatersInParty;
        }

        ControlHelper.Tooltip("Has no effect on cross-world parties, since those don't allow riding pillion.");

        ImGui.Indent();
        ImGui.BeginDisabled(!group.ForceMultiseatersInParty);
        bool preferMoreSeats = group.PreferMoreSeats;
        if (ImGui.Checkbox("Prefer mounts that can accomodate more party members", ref preferMoreSeats))
        {
            group.PreferMoreSeats = preferMoreSeats;
        }

        ControlHelper.Tooltip(
            "Requires the random mount to accomodate the largest number of current party members possible.",
            "E.g. if your party size is 4, only mounts with 4 or more seats are considered. However, if",
            "you don't have a 4-seater (or more), only mounts with the largest number of seats available",
            "to you is considered.");

        ImGui.EndDisabled();
        ImGui.Unindent();
    }

    private static void UpdateMountSelectionData(MountGroup group, List<MountData> unlockedMounts, bool enableNewMounts)
    {
        if (enableNewMounts == group.IncludedMeansActive)
        {
            // we auto-enable new mounts by tracking which mounts are explicitly disabled
            group.IncludedMeansActive = !enableNewMounts;

            // invert selection
            var unlockedMountIDs = unlockedMounts.Select(x => x.ID).ToHashSet();
            unlockedMountIDs.ExceptWith(group.IncludedMounts);
            group.IncludedMounts.Clear();
            group.IncludedMounts.UnionWith(unlockedMountIDs);
        }
    }

    private MountGroup SelectCurrentGroup(CharacterConfig characterConfig)
    {
        if (_currentMountGroup is not null && characterConfig.Groups.All(x => x.Name != _currentMountGroup))
        {
            _currentMountGroup = null;
        }

        _currentMountGroup ??= characterConfig.Groups.First().Name;

        ControlHelper.SelectItem(characterConfig.Groups, x => x.Name, ref _currentMountGroup, "##currentgroup", 150);

        string currentGroup = _currentMountGroup;
        ImGui.SameLine();
        if (ImGui.Button("Add"))
        {
            var dialog = new RenameItemDialog(
                _plugin.WindowManager,
                "Add a new group",
                string.Empty,
                x => AddMountGroup(characterConfig, x))
            {
                NormalizeWhitespace = true
            };

            dialog.SetValidation(x => ValidateGroup(x, isNew: true), x => "A group with that name already exists.");
            _plugin.WindowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        if (ImGui.Button("Edit"))
        {
            var dialog = new RenameItemDialog(
                _plugin.WindowManager,
                $"Rename {_currentMountGroup}",
                _currentMountGroup,
                (newName) => RenameMountGroup(_currentMountGroup, newName))
            {
                NormalizeWhitespace = true
            };

            dialog.SetValidation(x => ValidateGroup(x, isNew: false), x => "Another group with that name already exists.");

            _plugin.WindowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!characterConfig.HasNonDefaultGroups);
        if (ImGui.Button("Delete"))
        {
            _plugin.WindowManager.Confirm(
                "Confirm deletion of mount group",
                $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
                ("OK", () => DeleteMountGroup(currentGroup)),
                "Cancel");
        }

        ImGui.EndDisabled();

        return characterConfig.GetMountGroup(_currentMountGroup)!;

        bool ValidateGroup(string newName, bool isNew)
        {
            if (_plugin.CharacterConfig is not { } characterConfig)
            {
                return false;
            }

            HashSet<string> names = new(characterConfig.Groups.Select(x => x.Name), StringComparer.InvariantCultureIgnoreCase);

            if (!isNew)
            {
                _ = names.Remove(currentGroup);
            }

            return !names.Contains(newName);
        }
    }

    private void DeleteMountGroup(string name)
    {
        if (_plugin.CharacterConfig is not { } characterConfig)
        {
            return;
        }

        MountGroupManager.Delete(characterConfig, name);

        if (_currentMountGroup == name)
        {
            _currentMountGroup = null;
        }
    }

    private void RenameMountGroup(string currentMountGroup, string newName)
    {
        if (_plugin.CharacterConfig is not { } characterConfig)
        {
            return;
        }

        MountGroupManager.Rename(characterConfig, currentMountGroup, newName);

        if (_currentMountGroup == currentMountGroup)
        {
            _currentMountGroup = newName;
        }
    }

    private void AddMountGroup(CharacterConfig characterConfig, string name)
    {
        characterConfig.Groups.Add(new MountGroup { Name = name });
        _currentMountGroup = name;
    }
}
