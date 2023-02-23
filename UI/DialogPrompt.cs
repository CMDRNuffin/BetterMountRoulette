namespace BetterMountRoulette.UI;

using ImGuiNET;

internal sealed class DialogPrompt : IWindow
{
    private readonly WindowManager _manager;
    private readonly string _title;
    private readonly string _text;
    private readonly WindowManager.ButtonConfig[] _buttons;

    public DialogPrompt(WindowManager manager, string title, string text, WindowManager.ButtonConfig[] buttons)
    {
        _manager = manager;
        _title = title;
        _text = text;
        _buttons = buttons;
    }

    public void Draw()
    {
        bool isOpen = true;
        if (ImGui.Begin(_title, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            foreach (string line in _text.Split('\n'))
            {
                CenterText(line);
            }

            bool hasButton = false;
            foreach (WindowManager.ButtonConfig button in _buttons)
            {
                if (hasButton)
                {
                    ImGui.SameLine();
                }

                hasButton = true;
                if (ImGui.Button(button.Text))
                {
                    isOpen = false;
                    button.Execute?.Invoke();
                }
            }
        }

        ImGui.End();
        if (!isOpen)
        {
            _manager.Close(this);
        }
    }

    private static void CenterText(string text)
    {
        float windowWidth = ImGui.GetWindowSize().X;
        float textWidth = ImGui.CalcTextSize(text).X;

        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
        ImGui.Text(text);
    }
}
