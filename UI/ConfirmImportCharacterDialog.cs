namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;

using ImGuiNET;

internal sealed class ConfirmImportCharacterDialog(ConfirmImportCharacterDialog.ImportHandler importHandler)
    : DialogWindow("Better Mount Roulette", ImGuiWindowFlags.Modal)
{
    private readonly ImportHandler _importHandler = importHandler;

    private bool _skipAsking;

    public delegate void ImportHandler(bool import, bool rememberAnswer);

    public override void Draw()
    {
        ImGui.Text("You have no saved settings for this character.");
        ImGui.Text("Would you like to import legacy settings for this character?");
        _ = ImGui.Checkbox("Remember my answer and don't ask again.", ref _skipAsking);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("This can later be changed in the settings.");
        }

        bool? result = null;
        if (ImGui.Button("Yes"))
        {
            result = true;

        }

        ImGui.SameLine();
        if (ImGui.Button("No"))
        {
            result = false;
        }

        if (result is bool value)
        {
            _importHandler(value, _skipAsking);
            IsOpen = false;
        }
    }
}
