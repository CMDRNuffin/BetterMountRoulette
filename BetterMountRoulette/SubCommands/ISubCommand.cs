namespace BetterMountRoulette.SubCommands;

using BetterMountRoulette.Util;

internal interface ISubCommand
{
    string HelpMessage { get; }

    string CommandName { get; }

    public string? ParentCommand { get; }

    public string FullCommand { set; }

    public BetterMountRoulettePlugin Plugin { get; set; }

    public PluginServices Services { get; set; }

    void AddSubCommand(ISubCommand child);

    bool Execute(string[] parameter);
}
