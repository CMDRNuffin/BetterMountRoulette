namespace BetterMountRoulette.UI;

using BetterMountRoulette.Util;

using Dalamud.Interface.Windowing;

using System.Linq;
using BetterRouletteBase.UI;

internal sealed class WindowManager : WindowManagerBase
{
    private readonly PluginServices _services;

    public WindowManager(PluginServices services) : base(services.DalamudPluginInterface)
    {
        _services = services;
    }

    protected override Window GetOrCreateConfigWindow(out bool isNew)
    {
        ConfigWindow? configWindow = InternalWindows.Windows.OfType<ConfigWindow>().FirstOrDefault();
        if (configWindow is null)
        {
            isNew = true;
            configWindow = new(_services);
        }
        else
        {
            isNew = false;
        }

        return configWindow;
    }

    protected override Window? TryGetConfigWindow()
    {
        return InternalWindows.Windows.OfType<ConfigWindow>().FirstOrDefault();
    }
}
