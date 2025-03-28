namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;
using BetterMountRoulette.Util;

using Dalamud.Interface.Windowing;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class WindowManager(BetterMountRoulettePlugin plugin, PluginServices services)
{
    private readonly BetterMountRoulettePlugin _plugin = plugin;
    private readonly PluginServices _services = services;
    private readonly WindowSystem _windows = new();
    private readonly WindowSystem _dialogs = new();
    private readonly List<Window> _windowsToRemove = new();

    public void Draw()
    {
        // clean up closed windows
        // maybe todo: keep windows that we want to keep alive?
        foreach (Window window in _windowsToRemove)
        {
            Remove(window);
        }

        _windowsToRemove.Clear();
        _windowsToRemove.AddRange(_windows.Windows.Where(w => !w.IsOpen).Concat(_dialogs.Windows.Where(w => !w.IsOpen)));

        ImGui.BeginDisabled(_dialogs.Windows.Count > 0);
        _windows.Draw();
        ImGui.EndDisabled();

        _dialogs.Draw();
    }

    public void Add(Window window)
    {
        _windows.AddWindow(window);
    }

    public void Remove(Window window)
    {
        if (_dialogs.Windows.Contains(window))
        {
            _dialogs.RemoveWindow(window);
        }
        else
        {
            _windows.RemoveWindow(window);
        }

        if (window is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public void OpenDialog(DialogWindow window)
    {
        window.IsOpen = true;
        _dialogs.AddWindow(window);
    }

    public void OpenConfigWindow()
    {
        ConfigWindow? configWindow = _windows.Windows.OfType<ConfigWindow>().FirstOrDefault();
        if (configWindow != null)
        {
            configWindow.BringToFront();
            return;
        }

        configWindow = new ConfigWindow(_plugin, _services)
        {
            IsOpen = true
        };

        Add(configWindow);
    }

    public void Confirm(string title, string text, params ButtonConfig[] buttons)
    {
        OpenDialog(new DialogPrompt(title, text, buttons));
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
}
