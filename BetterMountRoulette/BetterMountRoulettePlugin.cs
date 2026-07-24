namespace BetterMountRoulette;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.SubCommands;
using BetterMountRoulette.UI;
using BetterMountRoulette.Util;

using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class BetterMountRoulettePlugin : IDalamudPlugin
{
    private bool _disposedValue;

    public const string COMMAND_TEXT = "/pbmr";

    public const string MOUNT_COMMAND_TEXT = "/pmount";

    ////public const string CommandHelpMessage = $"Does all the things. Type \"{CommandText} help\" for more information.";
    public const string COMMAND_HELP_MESSAGE = $"Open the config window.";

    private readonly PluginServices _services;
    private readonly ISubCommand _command;

    public unsafe BetterMountRoulettePlugin(IDalamudPluginInterface pluginInterface)
    {
        ArgumentNullException.ThrowIfNull(pluginInterface);

        Configuration config = pluginInterface.GetPluginConfig() as Configuration ?? Configuration.Init();
        ConfigVersionManager.DoMigration(config);

        _services = new PluginServices(pluginInterface, config);
        _services.SaveGlobalConfig();
        
        _services.Login += OnLogin;
        _ = _services.Framework.RunOnTick(() =>
        {
            if (_services.PlayerState.IsLoaded)
            {
                OnLogin(this, EventArgs.Empty);
            }
        });

        _command = InitCommands();
        _services.AddCommand(
            COMMAND_TEXT,
            new CommandInfo(HandleCommand) { HelpMessage = COMMAND_HELP_MESSAGE });
        _services.AddCommand(
            MOUNT_COMMAND_TEXT,
            new CommandInfo(_services.ActionHandler.HandleMountCommand)
            {
                HelpMessage = "Mount a random mount from the specified group, e.g. \"/pmount My Group\" summons a mount from the \"My Group\" group"
            });
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (_services.CharacterConfig is { EnableFlyingRouletteButton: bool enable, OpenWithNotebook: bool openWithNotebook })
        {
            if (!_services.GameFunctions.ToggleFlyingRouletteButton(enable))
            {
                // retry on the next frame if the sheet is not yet loaded
                return;
            }

            if (openWithNotebook)
            {
                _services.RegisterOpenWithNotebookCallback();
            }

            _services.Framework.Update -= OnFrameworkUpdate;
        }
    }

    private void OnLogin(object? sender, EventArgs e)
    {
        if (_services.PlayerState.IsLoaded)
        {
            _services.CharacterConfig = _services.CharacterManager.GetCharacterConfig(_services.PlayerState.ContentId);
            if (_services.CharacterConfig.IsNew && _services.Configuration.CharacterConfigs.ContainsKey(Configuration.DUMMY_LEGACY_CONFIG_ID))
            {
                ImportCharacterConfig();
                _services.CharacterConfig.IsNew = false;
            }

            _services.Framework.Update += OnFrameworkUpdate;
        }
    }

    private void ImportCharacterConfig(int? @override = null)
    {
        switch (@override ?? _services.Configuration.NewCharacterHandling)
        {
            case Configuration.NewCharacterHandlingModes.IMPORT:
                _ = _services.CharacterManager.Import(Configuration.DUMMY_LEGACY_CONFIG_ID);
                break;
            case Configuration.NewCharacterHandlingModes.BLANK:
                return;
            case Configuration.NewCharacterHandlingModes.ASK:
            default: /* default to "ask" for invalid values */
                _services.WindowManager.OpenDialog(new ConfirmImportCharacterDialog(ConfirmAction));
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
                _services.Configuration.NewCharacterHandling = mode;
            }
        }
    }

    private void HandleCommand(string command, string arguments)
    {
        // todo: correctly handle arguments, including
        // [/foo "bar"] being equal to [/foo bar] and the like
        string[] parts = string.IsNullOrEmpty(arguments) ? [] : arguments.Split(' ');
        try
        {
            bool success = _command.Execute(parts);

            if (!success)
            {
                _services.PluginLog.Error($"Invalid command: {command} {arguments}");
                if (!(_services.CharacterConfig?.SuppressChatErrors ?? false))
                {
                    _services.Chat.PrintError($"Invalid command: {command} {arguments}");
                }
            }
        }
        catch (Exception e) when (!(_services.CharacterConfig?.SuppressChatErrors ?? false))
        {
            _services.Chat.PrintError(e.Message);
            throw;
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

            _services.SaveGlobalConfig();
            _services.Dispose();

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
