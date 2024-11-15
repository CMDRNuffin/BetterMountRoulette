namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using ImGuiNET;

using Lumina.Excel.Sheets;

using System;
using System.Linq;
using System.Numerics;

internal sealed class ConfigWindow : IWindow
{
    private bool _isOpen;
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly Services _services;
    private readonly MountGroupPage _mountGroupPage;
    private readonly CharacterManagementRenderer _charManagementRenderer;
    private float _windowMinWidth;

    private static string[]? _mainCommands;

    public ConfigWindow(BetterMountRoulettePlugin plugin, Services services)
    {
        _plugin = plugin;
        _services = services;
        _mountGroupPage = new MountGroupPage(_plugin, services);
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
        ImGui.SetNextWindowSizeConstraints(new Vector2(_windowMinWidth, 0), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("Better Mount Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (_plugin.CharacterConfig is not CharacterConfig characterConfig)
            {
                ImGui.Text("Please log in first");
            }
            else if (ImGui.BeginTabBar("settings"))
            {
                Tab("General", GeneralConfigTab);
                Tab("Mount Groups", _mountGroupPage.RenderPage);
                Tab("Character Management", x => _charManagementRenderer.Draw());

                ImGui.EndTabBar();

                // Helper method for reducing boilerplate
                void Tab(string name, Action<CharacterConfig> contentSelector)
                {
                    if (ImGui.BeginTabItem(name))
                    {
                        contentSelector(characterConfig);
                        ImGui.EndTabItem();
                    }
                }
            }

            _windowMinWidth = ImGui.GetWindowWidth();
        }

        ImGui.End();

        if (!_isOpen)
        {
            _plugin.CharacterManager.SaveCurrentCharacterConfig();
            _plugin.SaveConfig(_plugin.Configuration);
            _plugin.WindowManager.Close(this);
        }
    }

    private void GeneralConfigTab(CharacterConfig characterConfig)
    {
        string? mountRouletteGroupName = characterConfig.MountRouletteGroup;
        string? flyingRouletteGroupName = characterConfig.FlyingMountRouletteGroup;

        bool revealMountsNormal = characterConfig.RevealMountsNormal;
        bool revealMountsFlying = characterConfig.RevealMountsFlying;

        RouletteGroup(characterConfig, ref mountRouletteGroupName, ref revealMountsNormal);
        RouletteGroup(characterConfig, ref flyingRouletteGroupName, ref revealMountsFlying, isFlying: true);

        ImGui.Text("For an override to take effect, the selected group has to enable at least one mount.");

        characterConfig.MountRouletteGroup = mountRouletteGroupName;
        characterConfig.FlyingMountRouletteGroup = flyingRouletteGroupName;
        characterConfig.RevealMountsNormal = revealMountsNormal;
        characterConfig.RevealMountsFlying = revealMountsFlying;

        // backwards compatibility
        _plugin.Configuration.Enabled = (mountRouletteGroupName ?? flyingRouletteGroupName) is not null;
    }

    private void RouletteGroup(CharacterConfig characterConfig, ref string? groupName, ref bool show, bool isFlying = false)
    {
        if (ImGui.BeginChildFrame(isFlying ? 2u : 1u, new Vector2(0, 60)))
        {
            if (ImGui.BeginTable($"##roulettegroup_{(isFlying ? "f" : "g")}", 2))
            {
                ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("##settings", ImGuiTableColumnFlags.WidthStretch);

                _ = ImGui.TableNextColumn();

                ImGui.Image(_services.TextureHelper.LoadIconTexture(isFlying ? 122u : 118u), new Vector2(50));

                _ = ImGui.TableNextColumn();

                SelectRouletteGroup(characterConfig, ref groupName, isFlying);

                _ = ImGui.Checkbox("Reveal mount in cast bar", ref show);

                ImGui.EndTable();
            }

            ImGui.EndChildFrame();
        }

        if (isFlying)
        {
            _mainCommands ??= _services.DataManager.GetExcelSheet<MainCommand>()!.Where(x => x.RowId is 3 or 61).Select(x => x.Name.ToString()).ToArray();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, ImGui.GetStyle().ItemSpacing.Y));
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();

                ImGui.Text("Flying Mount Roulette is available from the ");
                ImGui.SameLine();
                for (int i = 0; i < _mainCommands.Length; i++)
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1), _mainCommands[i]);
                    ImGui.SameLine();

                    if (i < _mainCommands.Length - 1)
                    {
                        ImGui.Text(" and ");
                        ImGui.SameLine();
                    }
                }

                ImGui.Text(" windows");
                ImGui.EndTooltip();
            }

            ImGui.PopStyleVar();
        }
    }

    private static void SelectRouletteGroup(CharacterConfig characterConfig, ref string? groupName, bool isFlying = false)
    {
        bool isEnabled = groupName is not null;

        _ = ImGui.Checkbox($"Replace with mount group", ref isEnabled);

        if (isEnabled)
        {
            groupName ??= characterConfig.Groups.FirstOrDefault()?.Name;

            if (groupName is not null)
            {
                ImGui.SameLine();
                SelectMountGroup(characterConfig, ref groupName, isFlying);
            }
        }
        else
        {
            groupName = null;
        }

        static void SelectMountGroup(CharacterConfig config, ref string group, bool isFlying)
        {
            ControlHelper.SelectItem(
                config.Groups,
                x => x.Name,
                ref group,
                $"##roulettegroup_{(isFlying ? "f" : "g")}", 100);
        }
    }
}
