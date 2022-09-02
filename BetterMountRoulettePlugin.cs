namespace BetterMountRoulette;

using BetterMountRoulette.Config;
using BetterMountRoulette.SubCommands;
using BetterMountRoulette.UI;
using BetterMountRoulette.Util;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class BetterMountRoulettePlugin : IDalamudPlugin
{
    private bool _disposedValue;

    public string Name => "Better Mount Roulette";

    public const string CommandText = "/pbmr";

    ////public const string CommandHelpMessage = $"Does all the things. Type \"{CommandText} help\" for more information.";
    public const string CommandHelpMessage = $"Open the config window.";

    [PluginService]
    internal static DalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;

    [PluginService]
    internal static SigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    internal static CommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public static GameGui GameGui { get; private set; } = null!;

    internal static GameData GameData => DataManager.GameData;

    [PluginService]
    public static DataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static ChatGui Chat { get; private set; } = null!;

    [PluginService]
    internal static Condition Condition { get; private set; } = null!;

    [PluginService]
    internal static ClientState ClientState { get; private set; } = null!;

    internal Configuration Configuration { get; private set; }

    private readonly Hook<UseActionHandler>? _useActionHook;

    internal readonly DebugWindow DebugWindow = new();
    internal readonly ConfigWindow ConfigWindow;
    internal ISubCommand _command;

    public unsafe BetterMountRoulettePlugin()
    {
        CastBarHelper.Plugin = this;

        ConfigWindow = new(this);
        ConfigWindow.Closed += SaveConfig;
        DalamudPluginInterface.UiBuilder.Draw += DebugWindow.Draw;
        DalamudPluginInterface.UiBuilder.Draw += ConfigWindow.Draw;

        try
        {
            Configuration config = DalamudPluginInterface.GetPluginConfig() as Configuration ?? Configuration.Init();
            DalamudPluginInterface.SavePluginConfig(config);

            Configuration = config;

            _command = InitCommands();
            Mounts.Instance.Load(config);

            DalamudPluginInterface.UiBuilder.OpenConfigUi += ConfigWindow.Open;

            _ = CommandManager.AddHandler(CommandText, new CommandInfo(HandleCommand) { HelpMessage = CommandHelpMessage });

            var renderAddress = (IntPtr)ActionManager.fpUseAction;

            if (renderAddress == IntPtr.Zero)
            {
                DebugWindow.Broken("Unable to load UseAction address");
                return;
            }

            _useActionHook = Hook<UseActionHandler>.FromAddress(renderAddress, OnUseAction);
            _useActionHook.Enable();
            CastBarHelper.Enable();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void SaveConfig(object? sender, EventArgs e)
    {
        DalamudPluginInterface.SavePluginConfig(Configuration);
    }

    private void HandleCommand(string command, string arguments)
    {
        // todo: correctly handle arguments, including
        // [/foo "bar"] being equal to [/foo bar] and the like
        var parts = string.IsNullOrEmpty(arguments) ? Array.Empty<string>() : arguments.Split(' ');
        try
        {
            bool success = _command.Execute(parts);

            if (!success)
            {
                Chat.PrintError($"Invalid command: {command} {arguments}");
            }
        }
        catch (Exception e)
        {
            Chat.PrintError(e.Message);
            throw;
        }
        finally
        {
            Chat.UpdateQueue();
        }
    }

    private ISubCommand InitCommands()
    {
        var allCommands = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.IsAssignableTo(typeof(ISubCommand)) && !x.IsAbstract && !x.IsInterface && x.GetConstructors().Any(x => x.GetParameters().Length == 0))
            .Select(x => (ISubCommand)Activator.CreateInstance(x)!)
            .ToList();
        Dictionary<string, ISubCommand> commands = new(StringComparer.InvariantCultureIgnoreCase);
        foreach (var command in allCommands)
        {
            var fullCommand = (command.ParentCommand + " " + command.CommandName).Trim();
            commands.Add(fullCommand, command);
            command.Plugin = this;
            command.FullCommand = (CommandText + " " + command.ParentCommand).Trim();
        }

        foreach (var command in allCommands)
        {
            commands[command.ParentCommand ?? string.Empty].AddSubCommand(command);
        }

        return commands[string.Empty];
    }

    private unsafe byte OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7)
    {
        // todo: if mounted (or mounted2), replace mount/mount roulette with dismount
        if (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2] || !Configuration.Enabled)
        {
            return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
        }

        var oldActionType = actionType;
        var oldActionId = actionID;
        if (actionType == ActionType.General && actionID == 9)
        {
            var newActionID = Mounts.Instance.GetRandom(actionManager);
            if (newActionID != 0)
            {
                actionType = ActionType.Mount;
                actionID = newActionID;
            }
        }

        switch (oldActionType)
        {
            case ActionType.General when oldActionId == 9 && actionType != oldActionType:
                CastBarHelper.Show = false;
                CastBarHelper.MountID = actionID;
                break;
            case ActionType.Mount:
                CastBarHelper.Show = true;
                CastBarHelper.MountID = actionID;
                break;
        }


        var result = _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);

        return result;
    }

    public unsafe delegate byte UseActionHandler(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID = 3758096384U, uint a4 = 0U, uint a5 = 0U, uint a6 = 0U, void* a7 = default);

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            Mounts.Instance.Save(Configuration);
            DalamudPluginInterface.SavePluginConfig(Configuration);

            _useActionHook?.Disable();
            _useActionHook?.Dispose();

            DalamudPluginInterface.UiBuilder.Draw -= DebugWindow.Draw;
            DalamudPluginInterface.UiBuilder.Draw -= ConfigWindow.Draw;
            DalamudPluginInterface.UiBuilder.OpenConfigUi -= ConfigWindow.Open;

            TextureHelper.Dispose();

            _ = CommandManager.RemoveHandler(CommandText);

            CastBarHelper.Plugin = null;
            CastBarHelper.Disable();
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
