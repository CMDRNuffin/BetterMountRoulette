namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using BetterRouletteBase.UI;
using BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;

using System;

internal sealed class MountGroupPage : ItemGroupPage<MountData, MountGroup, MountRegistry>
{
    private readonly BetterMountRoulettePlugin _plugin;

    internal MountGroupPage(BetterMountRoulettePlugin plugin, PluginServices services)
        : base(plugin.MountRegistry, services.TextureProvider, plugin.WindowManager, "mount")
    {
        _plugin = plugin;
    }

    protected override void PluginSpecificSettings(MountGroup group)
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
}