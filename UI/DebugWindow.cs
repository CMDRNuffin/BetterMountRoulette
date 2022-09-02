﻿namespace BetterMountRoulette.UI;

using ImGuiNET;

internal class DebugWindow
{
    private string? _text;

    private bool _isOpen;

    public DebugWindow()
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

        // title = "Action Debug", window identifier = "BetterMountRouletteDbg"
        _ = ImGui.Begin("Action Debug###BetterMountRouletteDbg", ref _isOpen);
        ImGui.Text(string.IsNullOrWhiteSpace(_text) ? "no text" : _text);
        ImGui.End();
    }

    internal void Clear()
    {
        _text = null;
    }
}