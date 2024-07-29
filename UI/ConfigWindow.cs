namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using ImGuiNET;

using System;
using System.Linq;
using System.Numerics;

internal sealed class ConfigWindow : IWindow
{
    private bool _isOpen;
    private readonly BetterMountRoulettePlugin _plugin;
    private readonly MountGroupPage _mountGroupPage;
    private readonly CharacterManagementRenderer _charManagementRenderer;
    private float _windowMinWidth;

    public ConfigWindow(BetterMountRoulettePlugin plugin, Services services)
    {
        _plugin = plugin;
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

        SelectRouletteGroup(characterConfig, ref mountRouletteGroupName);
        SelectRouletteGroup(characterConfig, ref flyingRouletteGroupName, isFlying: true);
        ImGui.Text("For one of these to take effect, the selected group has to enable at least one mount.");

        characterConfig.MountRouletteGroup = mountRouletteGroupName;
        characterConfig.FlyingMountRouletteGroup = flyingRouletteGroupName;

        // backwards compatibility
        _plugin.Configuration.Enabled = (mountRouletteGroupName ?? flyingRouletteGroupName) is not null;
    }

    private static void SelectRouletteGroup(CharacterConfig characterConfig, ref string? groupName, bool isFlying = false)
    {
        bool isEnabled = groupName is not null;
        _ = ImGui.Checkbox($"Enable for {(isFlying ? "Flying " : "")} Mount Roulette", ref isEnabled);
        if (isFlying)
        {
            ControlHelper.Tooltip(
                "Legacy action from when some mounts couldn't fly. "
                + "Currently available in game only via macro.");
        }

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
