namespace BetterMountRoulette.UI;

using ImGuiNET;

internal sealed class DialogPrompt(WindowManager manager, string title, string text, WindowManager.ButtonConfig[] buttons) : IWindow
{
    private readonly WindowManager _manager = manager;
    private readonly string _title = title;
    private readonly string _text = text;
    private readonly WindowManager.ButtonConfig[] _buttons = buttons;

    public void Draw()
    {
        bool isOpen = true;
        if (ImGui.Begin(_title, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            string[] lines = _text.Split('\n');
            float[] widths = new float[lines.Length];
            float largesWidth = 0;

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
                float width = ImGui.CalcTextSize(lines[i]).X;
                widths[i] = width;
                if (width > largesWidth)
                {
                    ImGui.Dummy(new(width, 0));
                    largesWidth = width;
                }
            }

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

    private static void CenterText(float textWidth)
    {
        float windowWidth = ImGui.GetWindowSize().X;

        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
    }
}
