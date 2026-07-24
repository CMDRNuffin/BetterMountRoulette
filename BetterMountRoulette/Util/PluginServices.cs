namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.UI;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;
using System.IO;

public sealed class PluginServices : IDisposable
{
    internal readonly IDalamudPluginInterface DalamudPluginInterface;
    private bool _disposedValue;
    private readonly HashSet<string> _commandNames = [];

    [PluginService]
    public ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public IDataManager DataManager { get; private set; } = null!;

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

    [PluginService]
    public IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    internal GameFunctions GameFunctions { get; }
    internal MountRegistry MountRegistry { get; }
    internal WindowManager WindowManager { get; }
    internal ActionHandler ActionHandler { get; }
    internal CharacterManager CharacterManager {get;}
    internal Configuration Configuration {get;}

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

    internal CharacterConfig? CharacterConfig
    {
        get => ActionHandler.CharacterConfig;
        set => ActionHandler.CharacterConfig = value;
    }

    internal PluginServices(IDalamudPluginInterface pluginInterface, Configuration config)
    {
        DalamudPluginInterface = pluginInterface;
        _ = pluginInterface.Inject(this);

        GameFunctions = new GameFunctions(this);
        MountRegistry = new MountRegistry(this);
        WindowManager = new WindowManager(this);
        ActionHandler = new ActionHandler(this, MountRegistry);
        Configuration = config;
        CharacterManager = new CharacterManager(this, config);
    }

    public void AddCommand(string command, CommandInfo commandInfo)
    {
        if (CommandManager.AddHandler(command, commandInfo))
        {
            _ = _commandNames.Add(command);
        }
    }

    public void RemoveCommand(string command)
    {
        _ = CommandManager.RemoveHandler(command);
        _ = _commandNames.Remove(command);
    }

    public void SaveGlobalConfig()
    {
        Configuration configuration = Configuration;
        if (configuration is null)
        {
            return;
        }

        try
        {
            DalamudPluginInterface.SavePluginConfig(configuration);
        }
        catch (IOException ex)
        {
            PluginLog.Error("Unable to save config: {0}", ex);
        }
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
                foreach(string command in _commandNames)
                {
                    _ = CommandManager.RemoveHandler(command);
                }
            }

            GameFunctions.Dispose();
            WindowManager.Dispose();
            ActionHandler.Dispose();

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

    internal void RegisterOpenWithNotebookCallback()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostOpen, "MountNoteBook", OnNotebookOpenClose);
        AddonLifecycle.RegisterListener(AddonEvent.PostClose, "MountNoteBook", OnNotebookOpenClose);
    }

    internal void ClearOpenWithNotebookCallback()
    {
        AddonLifecycle.UnregisterListener(OnNotebookOpenClose);
    }

    private void OnNotebookOpenClose(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostOpen:
                WindowManager.OpenConfigWindow();
                break;
            case AddonEvent.PostClose:
                WindowManager.CloseConfigWindow();
                break;
        }
    }
}
