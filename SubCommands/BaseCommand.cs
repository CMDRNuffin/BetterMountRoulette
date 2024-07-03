namespace BetterMountRoulette.SubCommands;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class BaseCommand : SubCommandBase
{
    private string? _helpMessage;

    public override string HelpMessage => _helpMessage ??= BuildHelpMessage();

    public override string CommandName => "";

    protected override bool ExecuteInternal(string[] parameter)
    {
        if (parameter.Length > 0)
        {
            return false;
        }

        Plugin.WindowManager.OpenConfigWindow();
        return true;
    }

    private string BuildHelpMessage()
    {
        StringBuilder sb = new StringBuilder()
            .AppendLine("Usage:")
            .AppendLine(FullCommand)
            .AppendLine("  -> opens the config window")
            .Append(FullCommand).AppendLine(" help")
            .Append("  -> prints this help");

        string[] modes = SubCommands.Keys.Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x.ToLower(CultureInfo.CurrentCulture)).ToArray();
        if (modes.Length == 0)
        {
            _ = sb.AppendLine()
                .Append(FullCommand).AppendLine(" <mode> [help]")
                .AppendLine("  -> executes the selected mode. Available modes are: ")
                .Append("  -> ")
                .AppendLine(string.Join(", ", modes))
                .AppendLine("  -> if the help parameter is present, displays additional information about the selected mode instead")
                .Append(CultureInfo.InvariantCulture, $"  -> e.g. {FullCommand} {modes[0]} help");
        }

        return sb.ToString();
    }
}
