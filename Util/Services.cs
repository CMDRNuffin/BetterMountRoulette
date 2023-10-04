namespace BetterMountRoulette.Util;

using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Lumina;

using System;

internal sealed class Services
{
    internal readonly DalamudPluginInterface DalamudPluginInterface;

    [PluginService]
    internal SigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    internal ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    public IDataManager DataManager { get; private set; } = null!;

    internal GameData GameData => DataManager.GameData;

    [PluginService]
    internal IChatGui Chat { get; private set; } = null!;

    [PluginService]
    internal ICondition Condition { get; private set; } = null!;

    [PluginService]
    internal IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal IGameInteropProvider GameInterop { get; private set; } = null!;

    [PluginService]
    internal IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    [PluginService]
    internal ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal IPluginLog PluginLog { get; private set; } = null!;

    internal TextureHelper TextureHelper { get; }

    private event EventHandler? LoginInternal;
    internal event EventHandler Login
    {
        add
        {
            if (value is null)
            {
                return;
            }

            LoginInternal += value;
            if (LoginInternal == value)
            {
                ClientState.Login += OnLogin;
            }
        }
        remove
        {
            LoginInternal -= value;
            if (LoginInternal == null)
            {
                ClientState.Login -= OnLogin;
            }
        }
    }

    public Services(DalamudPluginInterface pluginInterface)
    {
        DalamudPluginInterface = pluginInterface;
        _ = pluginInterface.Inject(this);
        TextureHelper = new(this);
    }

    private void OnLogin()
    {
        Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (ClientState.LocalPlayer is null)
        {
            return;
        }

        Framework.Update -= OnFrameworkUpdate;
        LoginInternal?.Invoke(this, EventArgs.Empty);
    }
}
