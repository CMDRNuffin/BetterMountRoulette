﻿namespace BetterMountRoulette.SubCommands;

using BetterMountRoulette.Util;

using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;

internal abstract class SubCommandBase : ISubCommand
{
    public abstract string HelpMessage { get; }

    public abstract string CommandName { get; }

    public virtual string? ParentCommand => null;

    public string FullCommand { get; set; } = null!;

    public BetterMountRoulettePlugin Plugin { get; set; } = null!;

    public PluginServices Services { get; set; } = null!;

    protected Dictionary<string, ISubCommand> SubCommands { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    protected IChatGui Chat => Services.Chat;

    public void AddSubCommand(ISubCommand child)
    {
        SubCommands.Add(child.CommandName, child);
    }

    public bool Execute(string[] parameter)
    {
        if (parameter.Length == 1 && "help".Equals(parameter[0], StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return true;
        }

        return parameter.Length >= 1 && SubCommands.TryGetValue(parameter[0], out ISubCommand? subCommand)
            ? subCommand.Execute(parameter[1..])
            : ExecuteInternal(parameter);
    }

    protected void PrintHelp()
    {
        Chat.Print(HelpMessage);
    }

    protected void DebugOutput(string message)
    {
        Chat.Print(message);
    }

    protected abstract bool ExecuteInternal(string[] parameter);
}
