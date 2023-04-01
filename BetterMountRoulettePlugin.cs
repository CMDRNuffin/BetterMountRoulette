namespace BetterMountRoulette;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.SubCommands;
using BetterMountRoulette.UI;
using BetterMountRoulette.Util;

using Dalamud.Game.Command;
using Dalamud.Plugin;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class BetterMountRoulettePlugin : IDalamudPlugin
{
    private bool _disposedValue;

    public string Name => "Better Mount Roulette";

    public const string COMMAND_TEXT = "/pbmr";

    public const string MOUNT_COMMAND_TEXT = "/pmount";

    ////public const string CommandHelpMessage = $"Does all the things. Type \"{CommandText} help\" for more information.";
    public const string COMMAND_HELP_MESSAGE = $"Open the config window.";

    internal readonly Configuration Configuration;

    internal CharacterConfig? CharacterConfig
    {
        get => _actionHandler.CharacterConfig;
        private set => _actionHandler.CharacterConfig = value;
    }

    private readonly Services _services;
    private readonly ActionHandler _actionHandler;
    internal readonly WindowManager WindowManager;
    internal readonly TextureHelper TextureHelper;
    internal readonly MountRegistry MountRegistry;
    internal readonly CharacterManager CharacterManager;
    private readonly ISubCommand _command;

    public unsafe BetterMountRoulettePlugin(DalamudPluginInterface pluginInterface)
    {
        if (pluginInterface is null)
        {
            throw new ArgumentNullException(nameof(pluginInterface));
        }

        _services = new Services(pluginInterface);
        TextureHelper = new TextureHelper(_services);
        MountRegistry = new MountRegistry(_services);

        WindowManager = new WindowManager(this, _services);
        pluginInterface.UiBuilder.Draw += WindowManager.Draw;

        try
        {
            _actionHandler = new ActionHandler(_services, WindowManager, MountRegistry);
            Configuration config = pluginInterface.GetPluginConfig() as Configuration ?? Configuration.Init();
            ConfigVersionManager.DoMigration(config);
            SaveConfig(config);

            Configuration = config;
            CharacterManager = new CharacterManager(_services, config);

            _services.Login += OnLogin;
            if (_services.ClientState.LocalPlayer is not null)
            {
                OnLogin(this, EventArgs.Empty);
            }

            _command = InitCommands();

            pluginInterface.UiBuilder.OpenConfigUi += WindowManager.OpenConfigWindow;

            _ = _services.CommandManager.AddHandler(
                COMMAND_TEXT,
                new CommandInfo(HandleCommand) { HelpMessage = COMMAND_HELP_MESSAGE });
            _ = _services.CommandManager.AddHandler(
                MOUNT_COMMAND_TEXT,
                new CommandInfo(_actionHandler.HandleMountCommand)
                {
                    HelpMessage = "Mount a random mount from the specified group, e.g. \"/pmount My Group\" summons a mount from the \"My Group\" group"
                });
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void ImportCharacterConfig(int? @override = null)
    {
        switch (@override ?? Configuration.NewCharacterHandling)
        {
            case Configuration.NewCharacterHandlingModes.IMPORT:
                _ = CharacterManager.Import(Configuration.DUMMY_LEGACY_CONFIG_ID);
                break;
            case Configuration.NewCharacterHandlingModes.BLANK:
                return;
            case Configuration.NewCharacterHandlingModes.ASK:
            default: /* default to "ask" for invalid values */
                WindowManager.OpenDialog(new ConfirmImportCharacterDialog(WindowManager, ConfirmAction));
                break;
        }

        void ConfirmAction(bool import, bool remember)
        {
            int mode = import
                ? Configuration.NewCharacterHandlingModes.IMPORT
                : Configuration.NewCharacterHandlingModes.BLANK;
            if (import)
            {
                ImportCharacterConfig(mode);
            }

            if (remember)
            {
                Configuration.NewCharacterHandling = mode;
            }
        }
    }

    private void OnLogin(object? sender, EventArgs e)
    {
        if (_services.ClientState.LocalPlayer is { } player)
        {
            CharacterConfig = CharacterManager.GetCharacterConfig(_services.ClientState.LocalContentId, player);
            if (CharacterConfig.IsNew && Configuration.CharacterConfigs.ContainsKey(Configuration.DUMMY_LEGACY_CONFIG_ID))
            {
                ImportCharacterConfig();
                CharacterConfig.IsNew = false;
            }
        }
    }

    internal void SaveConfig(Configuration configuration)
    {
        _services.DalamudPluginInterface.SavePluginConfig(configuration);
    }

    private void HandleCommand(string command, string arguments)
    {
        // todo: correctly handle arguments, including
        // [/foo "bar"] being equal to [/foo bar] and the like
        string[] parts = string.IsNullOrEmpty(arguments) ? Array.Empty<string>() : arguments.Split(' ');
        try
        {
            bool success = _command.Execute(parts);

            if (!success)
            {
                _services.Chat.PrintError($"Invalid command: {command} {arguments}");
            }
        }
        catch (Exception e)
        {
            _services.Chat.PrintError(e.Message);
            throw;
        }
        finally
        {
            _services.Chat.UpdateQueue();
        }
    }

    private ISubCommand InitCommands()
    {
        var allCommands = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.IsAssignableTo(typeof(ISubCommand)) && !x.IsAbstract && !x.IsInterface && x.GetConstructors().Any(x => x.GetParameters().Length == 0))
            .Select(x => (ISubCommand)Activator.CreateInstance(x)!)
            .ToList();
        Dictionary<string, ISubCommand> commands = new(StringComparer.InvariantCultureIgnoreCase);
        foreach (ISubCommand? command in allCommands)
        {
            string fullCommand = (command.ParentCommand + " " + command.CommandName).Trim();
            commands.Add(fullCommand, command);
            command.Plugin = this;
            command.FullCommand = (COMMAND_TEXT + " " + command.ParentCommand).Trim();
        }

        foreach (ISubCommand? command in allCommands)
        {
            commands[command.ParentCommand ?? string.Empty].AddSubCommand(command);
        }

        return commands[string.Empty];
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            SaveConfig(Configuration);

            _services.DalamudPluginInterface.UiBuilder.Draw -= WindowManager.Draw;
            _services.DalamudPluginInterface.UiBuilder.OpenConfigUi -= WindowManager.OpenConfigWindow;

            TextureHelper.Dispose();

            _ = _services.CommandManager.RemoveHandler(COMMAND_TEXT);
            _ = _services.CommandManager.RemoveHandler(MOUNT_COMMAND_TEXT);
            _actionHandler.Dispose();

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~BetterMountRoulettePlugin()
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
