namespace BetterMountRoulette.UI;

using ImGuiNET;

internal sealed class ConfirmImportCharacterDialog : IWindow
{
    private readonly WindowManager _windowManager;
    private readonly ImportHandler _importHandler;

    private bool _skipAsking;

    public delegate void ImportHandler(bool import, bool rememberAnswer);

    public ConfirmImportCharacterDialog(WindowManager windowManager, ImportHandler importHandler)
    {
        _windowManager = windowManager;
        _importHandler = importHandler;
    }

    public void Draw()
    {
        bool? result = null;
        bool open = true /* keep open until a choice was made */;
        if (ImGui.Begin("Import legacy data?", ref open, ImGuiWindowFlags.Modal))
        {
            ImGui.Text("You have no saved settings for this character.");
            ImGui.Text("Would you like to import legacy settings for this character?");
            _ = ImGui.Checkbox("Remember my answer and don't ask again.", ref _skipAsking);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This can later be changed in the settings.");
            }

            if (ImGui.Button("Yes"))
            {
                result = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                result = false;
            }
        }

        ImGui.End();

        if (result is bool res)
        {
            _windowManager.Close(this);
            _importHandler(res, _skipAsking);
        }
    }
}
