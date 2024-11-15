namespace BetterMountRoulette.UI;

using BetterMountRoulette.Util;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal sealed class WindowManager(BetterMountRoulettePlugin plugin, Services services)
{
    private readonly BetterMountRoulettePlugin _plugin = plugin;
    private readonly Services _services = services;
    private readonly WindowStack _windows = new();
    private readonly List<IWindow> _removeList = [];

    public void Draw()
    {
        _windows.Draw();

        foreach (IWindow item in _removeList)
        {
            _windows.Remove(item);
        }

        _removeList.Clear();
    }

    public void Open(IWindow window)
    {
        _windows.AddWindow(window);
    }

    public void Close(IWindow window)
    {
        _removeList.Add(window);
    }

    public void OpenDialog(IWindow window)
    {
        _windows.AddDialog(window);
    }

    public void OpenConfigWindow()
    {
        var configWindow = new ConfigWindow(_plugin, _services);
        if (_windows.Contains(configWindow))
        {
            return;
        }

        configWindow.Open();
        Open(configWindow);
    }

    public void Confirm(string title, string text, params ButtonConfig[] buttons)
    {
        OpenDialog(new DialogPrompt(this, title, text, buttons));
    }

    public void ConfirmYesNo(string title, string text, Action confirmed)
    {
        Confirm(title, text, ("Yes", confirmed), "No");
    }

    public readonly struct ButtonConfig
    {
        public readonly string Text;
        public readonly Action? Execute;

        private ButtonConfig(string text)
        {
            Text = text;
            Execute = null;
        }

        private ButtonConfig(string text, Action execute)
        {
            Text = text;
            Execute = execute;
        }

        public static implicit operator ButtonConfig(string text)
        {
            return new ButtonConfig(text);
        }

        public static implicit operator ButtonConfig((string text, Action execute) value)
        {
            return new(value.text, value.execute);
        }

        public static implicit operator ButtonConfig((Action execute, string text) value)
        {
            return new(value.text, value.execute);
        }
    }

    private sealed class WindowStack
    {
        private readonly List<(List<IWindow> Windows, bool IsDialog)> _windows = [];

        public bool Contains(IWindow window)
        {
            return _windows.Any(w => w.Windows.Any(x => Equals(x, window)));
        }

        public T? Get<T>() where T : IWindow
        {
            return _windows.SelectMany(w => w.Windows).OfType<T>().FirstOrDefault();
        }

        public void AddWindow(IWindow window)
        {
            if (_windows.Count == 0 || _windows.Last().IsDialog)
            {
                _windows.Add((new List<IWindow>(), false));
            }

            _windows.Last().Windows.Add(window);
        }

        public void AddDialog(IWindow window)
        {
            _windows.Add((new List<IWindow>(), true));
            _windows[^1].Windows.Add(window);
        }

        public void Remove(IWindow window)
        {
            for (int i = 0; i < _windows.Count; ++i)
            {
                for (int j = _windows[i].Windows.Count - 1; j < _windows[i].Windows.Count; ++j)
                {
                    if (window == _windows[i].Windows[j])
                    {
                        _windows[i].Windows.RemoveAt(j);
                        break;
                    }
                }

                if (_windows[i].Windows.Count == 0)
                {
                    _windows.RemoveAt(i);
                    --i;
                }
            }
        }

        public void Draw()
        {
            int highestDialogIndex = _windows.FindLastIndex(x => x.IsDialog);
            for (int i = 0; i < _windows.Count; ++i)
            {
                ImGui.BeginDisabled(i < highestDialogIndex);

                try
                {
                    bool isDialog = _windows[i].IsDialog;
                    foreach (IWindow window in _windows[i].Windows)
                    {
                        if (isDialog)
                        {
                            Vector2 mainViewportSize = ImGui.GetMainViewport().WorkSize;
                            ImGui.SetNextWindowPos(mainViewportSize / 2, ImGuiCond.Appearing, new Vector2(.5f));
                        }

                        window.Draw();
                    }
                }
                finally
                {
                    ImGui.EndDisabled();
                }
            }
        }
    }
}

internal interface IWindow
{
    void Draw();
}
