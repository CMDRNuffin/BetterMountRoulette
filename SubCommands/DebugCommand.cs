namespace BetterMountRoulette.SubCommands;

#if DEBUG
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal class DebugCommand : SubCommandBase
{
    public override string HelpMessage => "Shows the debug window";

    public override string CommandName => "dbg";

    protected override bool ExecuteInternal(string[] parameter)
    {
        if (parameter.Length == 1 && parameter[0] == "clr")
        {
            Plugin.DebugWindow.Clear();
            return true;
        }

        if (parameter.Length > 0)
        {
            return false;
        }

        Plugin.DebugWindow.Open();

        return true;
    }
}
#endif
