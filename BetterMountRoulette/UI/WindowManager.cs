namespace BetterMountRoulette.UI;

using BetterMountRoulette.Util;

using Dalamud.Interface.Windowing;

using System.Linq;
using BetterRouletteBase.UI;

internal sealed class WindowManager(BetterMountRoulettePlugin plugin, PluginServices services)
    : WindowManagerBase(services.DalamudPluginInterface)
{
    private readonly BetterMountRoulettePlugin _plugin = plugin;
    private readonly PluginServices _services = services;

    protected override Window GetOrCreateConfigWindow(out bool isNew)
    {
        ConfigWindow? configWindow = InternalWindows.Windows.OfType<ConfigWindow>().FirstOrDefault();
        if (configWindow is null)
        {
            isNew = true;
            configWindow = new(_plugin, _services);
        }
        else
        {
            isNew = false;
        }

        return configWindow;
    }
}
