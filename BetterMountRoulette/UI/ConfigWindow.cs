namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;

using Lumina.Excel.Sheets;

using System;
using System.Linq;
using System.Numerics;
using BetterRouletteBase.Util;
using BetterRouletteBase.UI;

internal sealed class ConfigWindow : ConfigWindowBase<CharacterConfig, MountGroup, MountData, MountRegistry, Configuration>
{
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly PluginServices _services;

    private static (uint RowId, string Name)[]? _mainCommands;

    public ConfigWindow(BetterMountRoulettePlugin plugin, PluginServices services)
        : base("Better Mount Roulette", ImGuiWindowFlags.AlwaysAutoResize)
    {
        _plugin = plugin;
        _services = services;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is ConfigWindow;
    }

    protected override WindowManagerBase WindowManager => _plugin.WindowManager;
    protected override MountRegistry ItemRegistry => _plugin.MountRegistry;
    protected override CharacterConfig? CharacterConfig => _plugin.CharacterConfig;
    protected override ReadOnlySpan<byte> ItemGroupsTabName => "Mount Groups"u8;
    protected override ItemGroupPage<MountData, MountGroup, MountRegistry> CreateItemGroupPage()
    {
        return new MountGroupPage(_plugin, _services);
    }

    protected override CharacterManagementRendererBase<Configuration> CreateCharacterManagementRenderer()
    {
        return new CharacterManagementRenderer(
            _services,
            _plugin.WindowManager,
            _plugin.CharacterManager,
            _plugin.Configuration);
    }
    protected override void Save()
    {
        _plugin.CharacterManager.SaveCurrentCharacterConfig();
        _plugin.SaveConfig(_plugin.Configuration);
    }

    protected override void GeneralConfigTab(CharacterConfig characterConfig)
    {
        string? mountRouletteGroupName = characterConfig.MountRouletteGroup;
        string? flyingRouletteGroupName = characterConfig.FlyingMountRouletteGroup;

        bool revealMountsNormal = characterConfig.RevealMountsNormal;
        bool revealMountsFlying = characterConfig.RevealMountsFlying;

        RouletteGroup(characterConfig, ref mountRouletteGroupName, ref revealMountsNormal);
        RouletteGroup(characterConfig, ref flyingRouletteGroupName, ref revealMountsFlying, isFlying: true);

        ImGui.Text("For an override to take effect, the selected group has to enable at least one mount."u8);

        EnableFlyingRouletteButtonCheckbox(characterConfig);

        bool suppressChatErrors = characterConfig.SuppressChatErrors;
        _ = ImGui.Checkbox("Suppress error messages in chat"u8, ref suppressChatErrors);

        characterConfig.MountRouletteGroup = mountRouletteGroupName;
        characterConfig.FlyingMountRouletteGroup = flyingRouletteGroupName;
        characterConfig.RevealMountsNormal = revealMountsNormal;
        characterConfig.RevealMountsFlying = revealMountsFlying;
        characterConfig.SuppressChatErrors = suppressChatErrors;

        // backwards compatibility
        _plugin.Configuration.Enabled = (mountRouletteGroupName ?? flyingRouletteGroupName) is not null;
    }

    private void EnableFlyingRouletteButtonCheckbox(CharacterConfig characterConfig)
    {
        bool enableFlyingRouletteButton = characterConfig.EnableFlyingRouletteButton;
        if (ImGui.Checkbox("Re-enable Flying Mount Roulette button"u8, ref enableFlyingRouletteButton))
        {
            characterConfig.EnableFlyingRouletteButton = enableFlyingRouletteButton;
            _ = _services.Framework.RunOnFrameworkThread(() => _services.GameFunctions.ToggleFlyingRouletteButton(enableFlyingRouletteButton));
        }

        if (ImGui.IsItemHovered())
        {
            _mainCommands ??= _services.DataManager.GetExcelSheet<MainCommand>()!
                .Where(x => x.RowId is 3 or 61).Select(x => (x.RowId, x.Name.ExtractText()))
                .ToArray();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, ImGui.GetStyle().ItemSpacing.Y));
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();

                ImGui.Text("Flying Mount Roulette is available from the "u8);
                ImGui.SameLine();
                for (int i = 0; i < _mainCommands.Length; i++)
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1), StringCache.MainCommands[_mainCommands[i].RowId, () => _mainCommands[i].Name]);
                    ImGui.SameLine();

                    if (i < _mainCommands.Length - 1)
                    {
                        ImGui.Text(" and "u8);
                        ImGui.SameLine();
                    }
                }

                ImGui.Text(" windows"u8);
                ImGui.EndTooltip();
            }

            ImGui.PopStyleVar();
        }
    }

    private void RouletteGroup(CharacterConfig characterConfig, ref string? groupName, ref bool show, bool isFlying = false)
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        const int ROWS = 2;
        float spacing = style.ItemSpacing.Y * (ROWS - 1);
        float checkboxHeight = ImGui.GetFrameHeight();
        float contentHeight = spacing + (checkboxHeight * ROWS);
        float totalHeight = contentHeight + (style.FramePadding.Y * 2) + style.ItemSpacing.Y;

        if (ImGui.BeginChildFrame(isFlying ? 2u : 1u, new Vector2(0, totalHeight)))
        {
            ReadOnlySpan<byte> rouletteGroupId = RouletteGroupID(isFlying);
            if (ImGui.BeginTable(rouletteGroupId, 2))
            {
                ImGui.TableSetupColumn("##icon"u8, ImGuiTableColumnFlags.WidthFixed, contentHeight);
                ImGui.TableSetupColumn("##settings"u8, ImGuiTableColumnFlags.WidthStretch);

                _ = ImGui.TableNextColumn();

                ImGui.Image(_services.TextureProvider.LoadIconTexture(isFlying ? 122u : 118u), new Vector2(contentHeight));

                _ = ImGui.TableNextColumn();

                SelectRouletteGroup(characterConfig, ref groupName, rouletteGroupId);

                _ = ImGui.Checkbox("Reveal mount in cast bar"u8, ref show);

                ImGui.EndTable();
            }

            ImGui.EndChildFrame();
        }
    }

    private static ReadOnlySpan<byte> RouletteGroupID(bool isFlying)
    {
        return isFlying
            ? "##roulettegroup_f"u8
            : "##roulettegroup_g"u8;
    }
}
