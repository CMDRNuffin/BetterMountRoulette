namespace BetterMountRoulette.UI;

using BetterRouletteBase.UI.Base;

using Dalamud.Bindings.ImGui;

internal sealed class ConfirmImportCharacterDialog(ConfirmImportCharacterDialog.ImportHandler importHandler)
    : DialogWindow("Better Mount Roulette", ImGuiWindowFlags.Modal)
{
    private readonly ImportHandler _importHandler = importHandler;

    private bool _skipAsking;

    public delegate void ImportHandler(bool import, bool rememberAnswer);

    public override void Draw()
    {
        ImGui.Text("You have no saved settings for this character."u8);
        ImGui.Text("Would you like to import legacy settings for this character?"u8);
        _ = ImGui.Checkbox("Remember my answer and don't ask again."u8, ref _skipAsking);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("This can later be changed in the settings."u8);
        }

        bool? result = null;
        if (ImGui.Button("Yes"u8))
        {
            result = true;

        }

        ImGui.SameLine();
        if (ImGui.Button("No"u8))
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
