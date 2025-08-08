namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;
using BetterMountRoulette.Util;

using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class WindowManager(BetterMountRoulettePlugin plugin, PluginServices services)
{
    private readonly BetterMountRoulettePlugin _plugin = plugin;
    private readonly PluginServices _services = services;
    private readonly WindowStack _windows = new();
    private readonly WindowStack _dialogs = new();

    public void Draw()
    {
        ImGui.BeginDisabled(_dialogs.HasWindows);
        _windows.Draw();
        ImGui.EndDisabled();

        _dialogs.Draw();
    }

    public void Add(Window window)
    {
        _windows.Add(window);
    }

    public void RemoveWindow(Window window)
    {
        RemoveWindowInternal(_windows, window);
    }

    public void RemoveDialog(Window window)
    {
        RemoveWindowInternal(_dialogs, window);
    }

    private static void RemoveWindowInternal(WindowStack system, Window window)
    {
        system.Remove(window);
    }

    public void OpenDialog(DialogWindow window)
    {
        window.IsOpen = true;
        _dialogs.Add(window);
    }

    public void OpenConfigWindow()
    {
        ConfigWindow? configWindow = _windows.Windows.OfType<ConfigWindow>().FirstOrDefault();
        if (configWindow != null)
        {
            configWindow.IsOpen = true;
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

    private sealed class WindowStack
    {
        private readonly List<Window> _windowsToRemove = new();
        private readonly WindowSystem _windows = new();

        public bool HasWindows => _windows.Windows.Count > 0;

        public IReadOnlyList<Window> Windows => _windows.Windows;

        public void Draw()
        {
            // clean up closed windows
            // maybe todo: keep windows that we want to keep alive?
            foreach (Window window in _windowsToRemove)
            {
                Remove(window);
            }

            _windowsToRemove.Clear();
            _windowsToRemove.AddRange(_windows.Windows.Where(x => !x.IsOpen));
            _windows.Draw();
        }

        public void Remove(Window window)
        {
            if (_windows.Windows.Contains(window))
            {
                // if window is still open, close it so any close action can happen
                if (window.IsOpen)
                {
                    window.IsOpen = false;
                }
                else
                {
                    _windows.RemoveWindow(window);
                    if (window is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public void Add(Window window)
        {
            _windows.AddWindow(window);
        }
    }
}
