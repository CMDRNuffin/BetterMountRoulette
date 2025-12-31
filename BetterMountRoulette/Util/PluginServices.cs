namespace BetterMountRoulette.Util;

using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Lumina;

using System;

public sealed class PluginServices : IDisposable
{
    internal readonly IDalamudPluginInterface DalamudPluginInterface;
    private bool _disposedValue;

    [PluginService]
    public ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public IDataManager DataManager { get; private set; } = null!;

    internal GameData GameData => DataManager.GameData;

    [PluginService]
    public IChatGui Chat { get; private set; } = null!;

    [PluginService]
    public ICondition Condition { get; private set; } = null!;

    [PluginService]
    public IClientState ClientState { get; private set; } = null!;

    [PluginService]
    public IPlayerState PlayerState { get; private set; } = null!;

    [PluginService]
    public IFramework Framework { get; private set; } = null!;

    [PluginService]
    public ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    public IPluginLog PluginLog { get; private set; } = null!;

    [PluginService]
    public IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    internal GameFunctions GameFunctions { get; }

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

    internal PluginServices(IDalamudPluginInterface pluginInterface)
    {
        DalamudPluginInterface = pluginInterface;
        _ = pluginInterface.Inject(this);

        GameFunctions = new GameFunctions(this);
    }

    private void OnLogin()
    {
        Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!PlayerState.IsLoaded)
        {
            return;
        }

        Framework.Update -= OnFrameworkUpdate;
        LoginInternal?.Invoke(this, EventArgs.Empty);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            GameFunctions.Dispose();
            _disposedValue = true;
        }
    }

    ~PluginServices()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
