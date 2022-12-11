namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

internal class ConfigWindow : IWindow
{
    private bool _isOpen;
    private readonly BetterMountRoulettePlugin _plugin;
    private string? _currentMountGroup;

    public ConfigWindow(BetterMountRoulettePlugin plugin)
    {
        _plugin = plugin;
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
        Mounts.RefreshUnlocked();
    }

    public void Draw()
    {
        if (ImGui.Begin("Better Mount Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTabBar("settings"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    string? mountRouletteGroupName = _plugin.Configuration.MountRouletteGroup;
                    string? flyingRouletteGroupName = _plugin.Configuration.FlyingMountRouletteGroup;

                    SelectRouletteGroup(ref mountRouletteGroupName);
                    SelectRouletteGroup(ref flyingRouletteGroupName, isFlying: true);
                    ImGui.Text("For one of these to take effect, the selected group has to enable at least one mount.");

                    _plugin.Configuration.MountRouletteGroup = mountRouletteGroupName;
                    _plugin.Configuration.FlyingMountRouletteGroup = flyingRouletteGroupName;

                    // backwards compatibility
                    _plugin.Configuration.Enabled = (mountRouletteGroupName ?? flyingRouletteGroupName) is not null;
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Mount groups"))
                {
                    var mounts = SelectCurrentGroup();
                    DrawMountGroup(mounts);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();
        ImGui.EndDisabled();

        if (!_isOpen)
        {
            BetterMountRoulettePlugin.DalamudPluginInterface.SavePluginConfig(_plugin.Configuration);
            _plugin.WindowManager.Close(this);
        }
    }

    private MountGroup SelectCurrentGroup()
    {
        var currentGroup = _currentMountGroup;
        _currentMountGroup ??= _plugin.Configuration.DefaultGroupName;

        SelectMountGroup(ref _currentMountGroup, "##currentgroup", 150);

        if (_currentMountGroup != currentGroup)
        {
            Mounts.GetInstance(_currentMountGroup)!.Filter(false, null, null);
        }

        int mode = 0;
        const int ModeAdd = 1;
        const int ModeEdit = 2;
        const int ModeDelete = 3;

        ImGui.SameLine();
        mode = ImGui.Button("Add") ? ModeAdd : mode;
        ImGui.SameLine();
        mode = ImGui.Button("Edit") ? ModeEdit : mode;
        ImGui.SameLine();
        ImGui.BeginDisabled(!_plugin.Configuration.Groups.Any());
        mode = ImGui.Button("Delete") ? ModeDelete : mode;
        ImGui.EndDisabled();

        currentGroup = _currentMountGroup;
        switch (mode)
        {
            case ModeAdd:
                var dialog = new RenameItemDialog(_plugin.WindowManager, "Add a new group", "", AddGroup);
                dialog.NormalizeWhitespace = true;
                dialog.SetValidation(x => ValidateGroup(x, isNew: true), x => "A group with that name already exists.");
                _plugin.WindowManager.OpenDialog(dialog);
                break;
            case ModeEdit:
                dialog = new RenameItemDialog(
                    _plugin.WindowManager,
                    $"Rename {_currentMountGroup}",
                    _currentMountGroup,
                    (newName) => RenameMountGroup(_currentMountGroup, newName));
                dialog.NormalizeWhitespace = true;
                dialog.SetValidation(x => ValidateGroup(x, isNew: false), x => "Another group with that name already exists.");

                _plugin.WindowManager.OpenDialog(dialog);
                break;
            case ModeDelete:
                _plugin.WindowManager.Confirm(
                    "Confirm deletion of mount group",
                    $"Are you sure you want to delete {currentGroup}?\nThis action can NOT be undone.",
                    ("OK", () => DeleteMountGroup(currentGroup)),
                    "Cancel");
                break;
        }

        if (_currentMountGroup == _plugin.Configuration.DefaultGroupName)
        {
            return new DefaultMountGroup(_plugin.Configuration);
        }
        else
        {
            return _plugin.Configuration.Groups.First(x => x.Name == _currentMountGroup);
        }

        bool ValidateGroup(string newName, bool isNew)
        {
            HashSet<string> names = new(_plugin.Configuration.Groups.Select(x => x.Name), StringComparer.InvariantCultureIgnoreCase)
            {
                _plugin.Configuration.DefaultGroupName
            };

            if (!isNew)
            {
                _ = names.Remove(currentGroup);
            }

            return !names.Contains(newName);
        }
    }

    private void DeleteMountGroup(string name)
    {
        if (name == _plugin.Configuration.DefaultGroupName)
        {
            var group = _plugin.Configuration.Groups.FirstOrDefault();
            if (group is null)
            {
                // can't delete the last group
                return;
            }

            _plugin.Configuration.DefaultGroupName = group.Name;
            _plugin.Configuration.EnabledMounts = group.EnabledMounts;
            _plugin.Configuration.IncludeNewMounts = group.IncludeNewMounts;
        }

        if (_plugin.Configuration.MountRouletteGroup == name)
        {
            _plugin.Configuration.MountRouletteGroup = _plugin.Configuration.DefaultGroupName;
        }

        if (_plugin.Configuration.FlyingMountRouletteGroup == name)
        {
            _plugin.Configuration.FlyingMountRouletteGroup = _plugin.Configuration.DefaultGroupName;
        }

        for (int i = 0; i < _plugin.Configuration.Groups.Count; ++i)
        {
            if (name == _plugin.Configuration.Groups[i].Name)
            {
                _plugin.Configuration.Groups.RemoveAt(i);
                break;
            }
        }

        Mounts.Remove(name);
    }

    private void RenameMountGroup(string currentMountGroup, string newName)
    {
        var config = _plugin.Configuration;
        if (config.MountRouletteGroup == currentMountGroup)
        {
            config.MountRouletteGroup = newName;
        }

        if (config.FlyingMountRouletteGroup == currentMountGroup)
        {
            config.FlyingMountRouletteGroup = newName;
        }

        if (config.DefaultGroupName == currentMountGroup)
        {
            config.DefaultGroupName = newName;
        }
        else
        {
            var group = config.Groups.First(x => x.Name == currentMountGroup);
            group.Name = newName;
        }

        Mounts.Remove(currentMountGroup);
        if (_currentMountGroup == currentMountGroup)
        {
            _currentMountGroup = newName;
            Mounts.GetInstance(newName)!.Filter(false, null, null);
        }
    }

    private void AddGroup(string name)
    {
        var config = _plugin.Configuration;
        config.Groups.Add(new MountGroup { Name = name });
        _currentMountGroup = name;
        var inst = Mounts.GetInstance(name)!;
        inst.Filter(false, null, null);
        inst.Update(true);
    }

    private void DrawMountGroup(MountGroup group)
    {
        var mounts = Mounts.GetInstance(group.Name)!;
        bool enableNewMounts = group.IncludeNewMounts;
        _ = ImGui.Checkbox("Enable new mounts on unlock", ref enableNewMounts);

        if (enableNewMounts != group.IncludeNewMounts)
        {
            group.IncludeNewMounts = enableNewMounts;
            mounts.UpdateUnlocked(enableNewMounts);
        }

        var pages = mounts.PageCount;
        if (pages == 0)
        {
            ImGui.Text("Please unlock at least one mount.");
        }
        else if (ImGui.BeginTabBar("mount_pages"))
        {
            for (int page = 1; page <= pages; page++)
            {
                if (ImGui.BeginTabItem($"{page}##mount_tab_{page}"))
                {
                    mounts.RenderItems(page);
                    mounts.Save(group);

                    var currentPage = page;
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
                        _plugin.WindowManager.ConfirmYesNo(
                            "Are you sure?",
                            $"Do you really want to {selectText}select all {pageInfo}?",
                            () =>
                            {
                                mounts.Update(info.Select, info.Page);
                                mounts.Save(group);
                            });
                    }

                    ImGui.EndTabItem();
                }
            }

            ImGui.SameLine();

            ImGui.EndTabBar();
        }
    }

    private void SelectRouletteGroup(ref string? groupName, bool isFlying = false)
    {
        bool isEnabled = groupName is not null;
        _ = ImGui.Checkbox($"Enable for {(isFlying ? "Flying " : "")} Mount Roulette", ref isEnabled);
        if (isEnabled)
        {
            groupName ??= _plugin.Configuration.DefaultGroupName;

            ImGui.SameLine();
            SelectMountGroup(ref groupName, $"##roulettegroup_{(isFlying ? "f" : "g")}", 100);
        }
        else
        {
            groupName = null;
        }
    }

    private void SelectMountGroup(ref string groupName, string label, float? width = null)
    {
        if (width is float w)
        {
            ImGui.SetNextItemWidth(w);
        }

        if (ImGui.BeginCombo(label, groupName))
        {
            if (ImGui.Selectable(_plugin.Configuration.DefaultGroupName, groupName == _plugin.Configuration.DefaultGroupName))
            {
                groupName = _plugin.Configuration.DefaultGroupName;
            }

            foreach (var group in _plugin.Configuration.Groups)
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
