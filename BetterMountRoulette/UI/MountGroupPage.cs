namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using BetterRouletteBase.UI;
using BetterRouletteBase.Util;
using ConditionalTooltipDisposable = BetterRouletteBase.UI.ControlHelper.ConditionalTooltipDisposable;

using Dalamud.Bindings.ImGui;

using System;

using Dalamud.Interface.Utility.Raii;
using ComboDisposable = Dalamud.Interface.Utility.Raii.ImRaii.ComboDisposable;

internal sealed class MountGroupPage : ItemGroupPage<MountData, MountGroup, MountRegistry>
{
    internal MountGroupPage(PluginServices services)
        : base(services.MountRegistry, services.TextureProvider, services.WindowManager, "mount")
    {
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

        using (ImRaii.PushIndent())
        using (ImRaii.Disabled(!forceMultiseatersInParty))
        {
            _ = ImGui.Checkbox("Prefer mounts that can accomodate more party members"u8, ref preferMoreSeats);
        }

        ControlHelper.Tooltip(
            "Requires the random mount to accomodate the largest number of current party members possible."u8
        );

        _ = ImGui.Checkbox("Use only single-seated mounts while solo"u8, ref forceSingleSeatersWhileSolo);
        ControlHelper.Tooltip("Also applies while in a cross-world party."u8);

        _ = ImGui.Checkbox("Use different settings for PvP (Frontline and Rival Wings)"u8, ref pvpOverride);
        using (ImRaii.PushIndent())
        using (ImRaii.Disabled(!pvpOverride))
        using (ImRaii.PushId(1))
        {
            _ = ImGui.Checkbox("Use only multi-seated mounts in parties"u8, ref pvpForceMultiseatersInParty);

            using (ImRaii.PushIndent())
            using (ImRaii.Disabled(!pvpForceMultiseatersInParty))
            {
                _ = ImGui.Checkbox("Prefer mounts that can accomodate more party members"u8, ref pvpPreferMoreSeats);
            }

            ControlHelper.Tooltip("Cosmetic only, since riding pillion is not available in PvP."u8);

            _ = ImGui.Checkbox("Use only single-seated mounts while solo"u8, ref pvpForceSingleSeatersWhileSolo);
        }

        _ = ImGui.Checkbox("Use highest ground speed mount"u8, ref fastMode);

        using (ConditionalTooltipDisposable tooltip = ControlHelper.Tooltip())
        {
            if (tooltip.Success)
            {
                string GetFastMountsText()
                {
                    string fastMountNames = string.Join("/", ItemRegistry.GetFastMountNames());
                    return $"Limits mount selection to {fastMountNames} in areas where increased";
                }

                ImGui.Text(StringCache.Named["FastMountsText", GetFastMountsText]);
                ImGui.Text("mount speed is available unless at least the first enhanced level of mount"u8);
                ImGui.Text("speed or flying is unlocked."u8);
                ImGui.Text("Requires at least one of these mounts to be unlocked and active to take effect."u8);
            }
        }

        using (ImRaii.PushIndent())
        using (ImRaii.Disabled(!fastMode))
        {
            _ = ImGui.Checkbox("Always use highest ground speed mount even if flight is unlocked"u8, ref fastModeAlways);
        }

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
        using ComboDisposable combo = ImRaii.Combo("##displayType"u8, DisplayTypeValue(displayType));
        if (combo.Success)
        {
            ComboItem(RouletteDisplayType.Grounded, ref displayType);
            ComboItem(RouletteDisplayType.Flying, ref displayType);
            ComboItem(RouletteDisplayType.Show, ref displayType);
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
                RouletteDisplayType.Show or _ => "Reveal mount during cast"u8,
            };
        }
    }
}