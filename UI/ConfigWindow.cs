namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class ConfigWindow : IWindow
{
    private bool _isOpen;
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly Services _services;
    private string? _currentMountGroup;
    private readonly MountRenderer _mountRenderer;
    private readonly CharacterManagementRenderer _charManagementRenderer;

    public ConfigWindow(BetterMountRoulettePlugin plugin, Services services)
    {
        _plugin = plugin;
        _services = services;
        _mountRenderer = new MountRenderer(_services);
        _charManagementRenderer = new CharacterManagementRenderer(
            services,
            _plugin.WindowManager,
            _plugin.CharacterManager,
            _plugin.Configuration);
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is ConfigWindow;
    }

    public void Open()
    {
        _isOpen = true;
        _plugin.MountRegistry.RefreshUnlocked();
    }

    public void Draw()
    {
        if (ImGui.Begin("Better Mount Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (_plugin.CharacterConfig is not CharacterConfig characterConfig)
            {
                ImGui.Text("Please log in first");
            }
            else if (ImGui.BeginTabBar("settings"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    string? mountRouletteGroupName = characterConfig.MountRouletteGroup;
                    string? flyingRouletteGroupName = characterConfig.FlyingMountRouletteGroup;

                    SelectRouletteGroup(characterConfig, ref mountRouletteGroupName);
                    SelectRouletteGroup(characterConfig, ref flyingRouletteGroupName, isFlying: true);
                    ImGui.Text("For one of these to take effect, the selected group has to enable at least one mount.");

                    characterConfig.MountRouletteGroup = mountRouletteGroupName;
                    characterConfig.FlyingMountRouletteGroup = flyingRouletteGroupName;

                    // backwards compatibility
                    _plugin.Configuration.Enabled = (mountRouletteGroupName ?? flyingRouletteGroupName) is not null;
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Mount groups"))
                {
                    MountGroup mounts = SelectCurrentGroup(characterConfig);
                    DrawMountGroup(mounts);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Character Management"))
                {
                    _charManagementRenderer.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();

        if (!_isOpen)
        {
            _plugin.CharacterManager.SaveCurrentCharacterConfig();
            _plugin.SaveConfig(_plugin.Configuration);
            _plugin.WindowManager.Close(this);
        }
    }

    private MountGroup SelectCurrentGroup(CharacterConfig characterConfig)
    {
        if(_currentMountGroup is not null && characterConfig.Groups.All(x => x.Name != _currentMountGroup))
        {
            _currentMountGroup = null;
        }

        _currentMountGroup ??= characterConfig.Groups.First().Name;

        SelectMountGroup(characterConfig, ref _currentMountGroup, "##currentgroup", 150);

        int mode = 0;
        const int MODE_ADD = 1;
        const int MODE_EDIT = 2;
        const int MODE_DELETE = 3;

        ImGui.SameLine();
        mode = ImGui.Button("Add") ? MODE_ADD : mode;
        ImGui.SameLine();
        mode = ImGui.Button("Edit") ? MODE_EDIT : mode;
        ImGui.SameLine();
        ImGui.BeginDisabled(!characterConfig.HasNonDefaultGroups);
        mode = ImGui.Button("Delete") ? MODE_DELETE : mode;
        ImGui.EndDisabled();

        string currentGroup = _currentMountGroup;
        switch (mode)
        {
            case MODE_ADD:
                var dialog = new RenameItemDialog(_plugin.WindowManager, "Add a new group", "", x => AddGroup(characterConfig, x))
                {
                    NormalizeWhitespace = true
                };

                dialog.SetValidation(x => ValidateGroup(x, isNew: true), x => "A group with that name already exists.");
                _plugin.WindowManager.OpenDialog(dialog);
                break;
            case MODE_EDIT:
                dialog = new RenameItemDialog(
                    _plugin.WindowManager,
                    $"Rename {_currentMountGroup}",
                    _currentMountGroup,
                    (newName) => RenameMountGroup(_currentMountGroup, newName))
                {
                    NormalizeWhitespace = true
                };

                dialog.SetValidation(x => ValidateGroup(x, isNew: false), x => "Another group with that name already exists.");

                _plugin.WindowManager.OpenDialog(dialog);
                break;
            case MODE_DELETE:
                _plugin.WindowManager.Confirm(
                    "Confirm deletion of mount group",
                    $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
                    ("OK", () => DeleteMountGroup(currentGroup)),
                    "Cancel");
                break;
        }

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

    private void AddGroup(CharacterConfig characterConfig, string name)
    {
        characterConfig.Groups.Add(new MountGroup { Name = name });
        _currentMountGroup = name;
    }

    private void DrawMountGroup(MountGroup group)
    {
        if (group is null)
        {
            ImGui.Text("Group is null!");
            return;
        }

        bool enableNewMounts = !group.IncludedMeansActive;
        _ = ImGui.Checkbox("Enable new mounts on unlock", ref enableNewMounts);
        
        bool forceMultiseatersInParty = group.ForceMultiseatersInParty;
        if (ImGui.Checkbox("Use only multiseater mounts in parties", ref forceMultiseatersInParty))
        {
            group.ForceMultiseatersInParty = forceMultiseatersInParty;
        }

        List<MountData> unlockedMounts = _plugin.MountRegistry.GetUnlockedMounts();
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
                    _mountRenderer.RenderPage(unlockedMounts, group.IncludedMounts, group.IncludedMeansActive, page);

                    int currentPage = page;
                    (bool Select, int? Page)? maybeInfo =
                        Buttons("Select all", "Unselect all", "Select page", "Unselect page") switch
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
#pragma warning disable IDE0053 // Use expression body for lambda expressions
                        // commented-out code needs to be preserved (for now)
                        _plugin.WindowManager.ConfirmYesNo(
                            "Are you sure?",
                            $"Do you really want to {selectText} all {pageInfo}?",
                            () =>
                            {
                                List<MountData> unlockedMounts = _plugin.MountRegistry.GetUnlockedMounts();
                                MountRenderer.Update(
                                    unlockedMounts,
                                    group.IncludedMounts,
                                    info.Select == group.IncludedMeansActive,
                                    info.Page);
                            });
#pragma warning restore IDE0053 // Use expression body for lambda expressions
                    }

                    ImGui.EndTabItem();
                }
            }

            ImGui.SameLine();

            ImGui.EndTabBar();
        }
    }

    private static void SelectRouletteGroup(CharacterConfig characterConfig, ref string? groupName, bool isFlying = false)
    {
        bool isEnabled = groupName is not null;
        _ = ImGui.Checkbox($"Enable for {(isFlying ? "Flying " : "")} Mount Roulette", ref isEnabled);
        if (isFlying && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(
                "Legacy action from when some mounts couldn't fly. "
                + "Currently available in game only via macro.");
        }

        if (isEnabled)
        {
            groupName ??= characterConfig.Groups.FirstOrDefault()?.Name;

            if (groupName is not null)
            {
                ImGui.SameLine();
                SelectMountGroup(characterConfig, ref groupName, $"##roulettegroup_{(isFlying ? "f" : "g")}", 100);
            }
        }
        else
        {
            groupName = null;
        }
    }

    private static void SelectMountGroup(CharacterConfig characterConfig, ref string groupName, string label, float? width = null)
    {
        if (width is float w)
        {
            ImGui.SetNextItemWidth(w);
        }

        if (ImGui.BeginCombo(label, groupName))
        {
            foreach (MountGroup group in characterConfig.Groups)
            {
                if (ImGui.Selectable(group.Name, group.Name == groupName))
                {
                    groupName = group.Name;
                }
            }

            ImGui.EndCombo();
        }
    }

    private static int? Buttons(params string[] buttons)
    {
        int? result = null;
        for (int i = 0; i < buttons.Length; ++i)
        {
            if (i > 0)
            {
                ImGui.SameLine();
            }

            if (ImGui.Button(buttons[i]))
            {
                result = i;
            }
        }

        return result;
    }
}
