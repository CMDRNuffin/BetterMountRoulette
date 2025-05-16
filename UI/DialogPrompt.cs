namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;

using ImGuiNET;

internal sealed class DialogPrompt(string title, string text, WindowManager.ButtonConfig[] buttons)
    : DialogWindow(title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings)
{
    private readonly string _text = text;
    private readonly WindowManager.ButtonConfig[] _buttons = buttons;

    public override void Draw()
    {
        string[] lines = _text.Split('\n');
        float[] widths = new float[lines.Length];
        float largestWidth = 0;

        // step 1: reserve width so stuff like
        //   > short line
        //   > very long line that stretches the window beyond what the title bar requires
        // doesn't end up looking exactly like here. Even though we probably won't ever need it.
        //
        // maybe idea for the future to also allow centering when the buttons are longer than the text:
        //   render buttons before text (store cursor pos, reserve entire text rectangle instead of width,
        //   draw buttons, restore cursor, draw text)
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
            float width = ImGui.CalcTextSize(lines[i]).X + 10;
            widths[i] = width;
            if (width > largestWidth)
            {
                largestWidth = width;
            }
        }

        ImGui.Dummy(new(largestWidth, 0));

        // step 2: actually draw the lines
        for (int i = 0; i < lines.Length; i++)
        {
            CenterText(widths[i]);
            ImGui.Text(lines[i]);
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
                IsOpen = false;
                button.Execute?.Invoke();
            }
        }
    }

    private static void CenterText(float textWidth)
    {
        float windowWidth = ImGui.GetWindowSize().X;

        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
    }
}
