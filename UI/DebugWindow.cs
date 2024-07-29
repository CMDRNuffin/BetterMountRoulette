namespace BetterMountRoulette.UI;

using BetterMountRoulette.Util;

using ImGuiNET;

internal sealed class DebugWindow
{
    private string? _text;

    private bool _isOpen;

    public DebugWindow(Services _)
    {
    }

    public void Open()
    {
        _isOpen = true;
    }

    public void Text(string text)
    {
        _text = text;
    }

    public void AddText(string text)
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            Text(text);
            return;
        }

        _text += "\r\n" + text;
    }

    public void Broken(string? sig)
    {
        _text = $"BROKEN: {sig ?? "NULL"}";
        Open();
    }

    public void Draw()
    {
        if (!_isOpen)
        {
            return;
        }

        if (ImGui.Begin("Action Debug###BetterMountRouletteDbg", ref _isOpen))
        {
            ImGui.Text(string.IsNullOrWhiteSpace(_text) ? "no text" : _text);
        }

        ImGui.End();
    }

    internal void Clear()
    {
        _text = null;
    }
}
