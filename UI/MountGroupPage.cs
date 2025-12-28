namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;
using BetterMountRoulette.Util.Memory;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class MountGroupPage
{
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly MountRenderer _mountRenderer;
    private string? _currentMountGroup;
    private MountGroupPageEnum _mode = MountGroupPageEnum.Settings;

    private string _nameFilter = "";
    private List<MountData>? _filteredMounts;
    private (int UnlockedCount, string Text) _lastFilter;

    private enum MountGroupPageEnum
    {
        Settings,
        Mounts
    }

    internal MountGroupPage(BetterMountRoulettePlugin plugin, PluginServices services)
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
            ImGui.Text("Group is null!"u8);
            return;
        }

        bool isSettingsOpen = _mode == MountGroupPageEnum.Settings;
        bool isMountsOpen = _mode == MountGroupPageEnum.Mounts;
        bool enableNewMounts = !group.IncludedMeansActive;

        ImGui.GetStateStorage().SetInt(ImGui.GetID("Settings"u8), isSettingsOpen ? 1 : 0);
        ImGui.BeginDisabled(isSettingsOpen);
        if (ImGui.CollapsingHeader("Settings"u8))
        {
            ImGui.EndDisabled();
            isSettingsOpen = true;
            RenderGroupSettings(group, ref enableNewMounts);
        }
        else
        {
            ImGui.EndDisabled();
        }

        List<MountData> unlockedMounts = _plugin.MountRegistry.GetUnlockedMounts();
        UpdateMountSelectionData(group, unlockedMounts, enableNewMounts);

        ImGui.GetStateStorage().SetInt(ImGui.GetID("Mounts"u8), isMountsOpen ? 1 : 0);
        ImGui.BeginDisabled(isMountsOpen);
        if (ImGui.CollapsingHeader("Mounts"u8))
        {
            ImGui.EndDisabled();
            isMountsOpen = true;

            StringView nameFilter = new();
            if (unlockedMounts.Count > 0)
            {
                nameFilter = DrawNameFilter();
            }

            List<MountData> filteredAndUnlockedMounts = ApplyFilterAndGetFilteredMounts(unlockedMounts, nameFilter);

            int pages = MountRenderer.GetPageCount(filteredAndUnlockedMounts.Count);
            if (pages == 0)
            {
                ImGui.Text(
                    unlockedMounts.Count == 0
                        ? "Please unlock at least one mount."u8
                        : "No mounts match the current filter."u8
                );
            }
            else if (ImGui.BeginTabBar("mount_pages"u8))
            {
                for (int page = 1; page <= pages; page++)
                {
                    if (ImGui.BeginTabItem(StringCache.Pages[page, () => $"{page}"]))
                    {
                        RenderMountListPage(page, group, filteredAndUnlockedMounts);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.SameLine();
                ImGui.EndTabBar();
            }
        }
        else
        {
            ImGui.EndDisabled();
        }

        switch (_mode)
        {
            case MountGroupPageEnum.Settings when isMountsOpen:
                _mode = MountGroupPageEnum.Mounts;
                break;
            case MountGroupPageEnum.Mounts when isSettingsOpen:
                _mode = MountGroupPageEnum.Settings;
                break;
            case MountGroupPageEnum.Settings:
            case MountGroupPageEnum.Mounts:
                break;
            default:
                // Something somewhere went horribly wrong. Reset to settings.
                _mode = MountGroupPageEnum.Settings;
                break;
        }
    }

    private StringView DrawNameFilter()
    {
        ImGui.SetNextItemWidth(250);

        _ = ImGui.InputTextWithHint("###nameFilter"u8, "Search for name..."u8, ref _nameFilter);

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FilterCircleXmark))
        {
            _nameFilter = string.Empty;
        }

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGui.SetTooltip("Clear name filter"u8);
            }
        }

        return new StringView(_nameFilter).Trim();
    }

    private void RenderMountListPage(int page, MountGroup group, List<MountData> unlockedAndFilteredMounts)
    {
        _mountRenderer.RenderPage(unlockedAndFilteredMounts, group, page);

        (bool Select, int? Page)? maybeInfo = null;

        Button("Select all"u8, ref maybeInfo, (true, null));
        ImGui.SameLine();
        Button("Unselect all"u8, ref maybeInfo, (false, null));
        ImGui.SameLine();
        Button("Select page"u8, ref maybeInfo, (true, page));
        ImGui.SameLine();
        Button("Unselect page"u8, ref maybeInfo, (false, page));

        if (maybeInfo is { } info)
        {
            string selectText = info.Select ? "select" : "unselect";
            string pageInfo = (info.Page, info.Select) switch
            {
                (null, true) => _nameFilter.IsNullOrEmpty()
                    ? "currently unselected mounts"
                    : $"mounts matching \"{_nameFilter}\"",
                (null, false) => _nameFilter.IsNullOrEmpty()
                    ? "currently selected mounts"
                    : $"mounts matching \"{_nameFilter}\"",
                _ => "mounts on the current page",
            };

            _plugin.WindowManager.ConfirmYesNo(
                "Are you sure?",
                $"Do you really want to {selectText} all {pageInfo}?",
                () => MountRenderer.Update(
                    unlockedAndFilteredMounts,
                    group,
                    info.Select,
                    info.Page
                )
            );
        }

        static void Button(ReadOnlySpan<byte> label, ref (bool, int?)? maybeInfo, (bool, int?) value)
        {
            if (ImGui.Button(label))
            {
                maybeInfo = value;
            }
        }
    }

    private void RenderGroupSettings(MountGroup group, ref bool enableNewMounts)
    {
        bool forceMultiseatersInParty = group.ForceMultiseatersInParty;
        bool preferMoreSeats = group.PreferMoreSeats;
        bool forceSingleSeatersWhileSolo = group.ForceSingleSeatersWhileSolo;
        bool pvpOverride = group.PvpOverrideMultiseaterSettings;
        bool pvpForceMultiseatersInParty = group.PvpForceMultiseatersInParty;
        bool pvpPreferMoreSeats = group.PvpPreferMoreSeats;
        bool pvpForceSingleSeatersWhileSolo = group.PvpForceSingleSeatersWhileSolo;
        bool fastMode = group.FastMode != FastMode.Off;
        bool fastModeAlways = group.FastMode == FastMode.On;
        RouletteDisplayType displayType = group.DisplayType;

        _ = ImGui.Checkbox("Enable new mounts on unlock", ref enableNewMounts);

        _ = ImGui.Checkbox("Use only multi-seated mounts in parties"u8, ref forceMultiseatersInParty);
        ControlHelper.Tooltip("Has no effect on cross-world parties, since those don't allow riding pillion."u8);

        ImGui.Indent();
        ImGui.BeginDisabled(!forceMultiseatersInParty);
        _ = ImGui.Checkbox("Prefer mounts that can accomodate more party members"u8, ref preferMoreSeats);
        ImGui.EndDisabled();
        ImGui.Unindent();

        ControlHelper.Tooltip(
            "Requires the random mount to accomodate the largest number of current party members possible."u8
        );

        _ = ImGui.Checkbox("Use only single-seated mounts while solo"u8, ref forceSingleSeatersWhileSolo);
        ControlHelper.Tooltip("Also applies while in a cross-world party."u8);

        _ = ImGui.Checkbox("Use different settings for PvP (Frontline and Rival Wings)"u8, ref pvpOverride);
        ImGui.Indent();
        ImGui.BeginDisabled(!pvpOverride);
        ImGui.PushID(1);
        _ = ImGui.Checkbox("Use only multi-seated mounts in parties"u8, ref pvpForceMultiseatersInParty);

        ImGui.Indent();
        ImGui.BeginDisabled(!pvpForceMultiseatersInParty);
        _ = ImGui.Checkbox("Prefer mounts that can accomodate more party members"u8, ref pvpPreferMoreSeats);
        ImGui.EndDisabled();
        ImGui.Unindent();

        _ = ImGui.Checkbox("Use only single-seated mounts while solo"u8, ref pvpForceSingleSeatersWhileSolo);

        ImGui.PopID();
        ImGui.EndDisabled();
        ImGui.Unindent();

        _ = ImGui.Checkbox("Use highest ground speed mount"u8, ref fastMode);

        if (ControlHelper.BeginTooltip())
        {
            string GetFastMountsText()
            {
                string fastMountNames = string.Join("/", _plugin.MountRegistry.GetFastMountNames());
                return $"Limits mount selection to {fastMountNames} in areas where increased";
            }

            ImGui.Text(StringCache.Named["FastMountsText", GetFastMountsText]);
            ImGui.Text("mount speed is available unless at least the first enhanced level of mount"u8);
            ImGui.Text("speed or flying is unlocked."u8);
            ImGui.Text("Requires at least one of these mounts to be unlocked and active to take effect."u8);
            ImGui.EndTooltip();
        }

        ImGui.Indent();
        ImGui.BeginDisabled(!fastMode);
        _ = ImGui.Checkbox("Always use highest ground speed mount even if flight is unlocked"u8, ref fastModeAlways);
        ImGui.EndDisabled();
        ImGui.Unindent();

        ControlHelper.Tooltip("Limits mount selection regardless of flying unlock status."u8);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("/pmount Behavior:"u8);
        ImGui.SameLine();
        SelectDisplayType(ref displayType);

        group.DisplayType = displayType;
        group.ForceMultiseatersInParty = forceMultiseatersInParty;
        group.PreferMoreSeats = preferMoreSeats;
        group.ForceSingleSeatersWhileSolo = forceSingleSeatersWhileSolo;
        group.PvpOverrideMultiseaterSettings = pvpOverride;
        group.PvpForceMultiseatersInParty = pvpForceMultiseatersInParty;
        group.PvpPreferMoreSeats = pvpPreferMoreSeats;
        group.PvpForceSingleSeatersWhileSolo = pvpForceSingleSeatersWhileSolo;
        group.FastMode = fastModeAlways
            ? FastMode.On
            : fastMode
                ? FastMode.IfGrounded
                : FastMode.Off;
    }

    private List<MountData> ApplyFilterAndGetFilteredMounts(List<MountData> unlockedMounts, StringView filter)
    {
        if (filter.Length == 0)
        {
            return unlockedMounts;
        }

        if (_filteredMounts is null
            || unlockedMounts.Count != _lastFilter.UnlockedCount
            || !filter.Equals(_lastFilter.Text, StringComparison.OrdinalIgnoreCase))
        {
            _filteredMounts = unlockedMounts
                .Where(
                    mountData => mountData.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
        }

        _lastFilter = (UnlockedCount: unlockedMounts.Count, Text: _nameFilter);

        return _filteredMounts;
    }

    private static void SelectDisplayType(ref RouletteDisplayType displayType)
    {
        if (ImGui.BeginCombo("##displayType"u8, DisplayTypeValue(displayType)))
        {
            ComboItem(RouletteDisplayType.Grounded, ref displayType);
            ComboItem(RouletteDisplayType.Flying, ref displayType);
            ComboItem(RouletteDisplayType.Show, ref displayType);

            ImGui.EndCombo();
        }

        static void ComboItem(RouletteDisplayType value, ref RouletteDisplayType selectedValue)
        {
            if (ImGui.Selectable(DisplayTypeValue(value), value == selectedValue))
            {
                selectedValue = value;
            }
        }

        static ReadOnlySpan<byte> DisplayTypeValue(RouletteDisplayType displayType)
        {
            return displayType switch
            {
                RouletteDisplayType.Grounded => "Show as Mount Roulette"u8,
                RouletteDisplayType.Flying => "Show as Flying Mount Roulette"u8,
                RouletteDisplayType.Show => "Reveal mount during cast"u8,
                _ => StringCache.Named[$"RouletteDisplayType_{displayType}", displayType.ToString],
            };
        }
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

        ControlHelper.SelectItem(characterConfig.Groups, x => x.Name, ref _currentMountGroup, "##currentgroup"u8, 150);

        string currentGroup = _currentMountGroup;
        ImGui.SameLine();
        if (ImGui.Button("Add"u8))
        {
            var dialog = new RenameItemDialog(
                "Add a new group",
                string.Empty,
                x => AddMountGroup(characterConfig, x)
            ) { NormalizeWhitespace = true };

            dialog.SetValidation(CreateValidator(isNew: true), x => "A group with that name already exists."u8);
            _plugin.WindowManager.OpenDialog(dialog);
        }

        ImGui.SameLine();
        if (ImGui.Button("Edit"))
        {
            var dialog = new RenameItemDialog(
                $"Rename {_currentMountGroup}",
                _currentMountGroup,
                (newName) => RenameMountGroup(_currentMountGroup, newName)
            ) { NormalizeWhitespace = true };

            dialog.SetValidation(
                CreateValidator(isNew: false),
                x => "Another group with that name already exists."u8
            );

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
                "Cancel"
            );
        }

        ImGui.EndDisabled();

        return characterConfig.GetMountGroup(_currentMountGroup)!;

        Func<StringView, bool> CreateValidator(bool isNew)
        {
            if (_plugin.CharacterConfig is not { } characterConfig)
            {
                return x => true;
            }

            HashSet<StringView> names = new(
                characterConfig.Groups.Select(x => new StringView(x.Name)),
                StringViewComparer.InvariantCultureIgnoreCase
            );

            if (!isNew)
            {
                _ = names.Remove(currentGroup);
            }

            return newName => !names.Contains(newName);
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