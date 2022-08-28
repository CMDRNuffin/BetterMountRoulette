namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;

using ImGuiNET;

using System;

internal class ConfigWindow
{
    private bool _isOpen;
    private bool _wasOpen;
    private bool _isEnabled;
    private readonly BetterMountRoulettePlugin _plugin;
    private string? _confirmationText;
    private Action? _confirmedAction;

    public ConfigWindow(BetterMountRoulettePlugin plugin)
    {
        _plugin = plugin;
    }

    public event EventHandler? Closed;

    public void Open()
    {
        _isOpen = true;
        Mounts.Instance.RefreshUnlocked();
        Mounts.Instance.Load(_plugin.Configuration);
        Mounts.Instance.Filter(false, null, null);
    }

    private void ConfirmDialog()
    {
        if (_confirmationText is null || _confirmedAction is null)
        {
            return;
        }

        var viewPort = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewPort.WorkSize / 2, ImGuiCond.Appearing, new(0.5f));

        _ = ImGui.Begin("PLease confirm", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
        ImGui.Text(_confirmationText);
        bool done = false;
        if (ImGui.Button("Confirm"))
        {
            done = true;
            _confirmedAction();
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel"))
        {
            done = true;
        }

        if (done)
        {
            _confirmationText = null;
            _confirmedAction = null;
        }
    }

    private void RequestConfirmation(string text, Action action)
    {
        _confirmationText = text;
        _confirmedAction = action;
    }

    public void Draw()
    {
        if (!_isOpen)
        {
            if (_wasOpen)
            {
                Mounts.Instance.Save(_plugin.Configuration);
                _wasOpen = false;

                Closed?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        bool disabled = _confirmationText is not null && _confirmedAction is not null;
        ImGui.BeginDisabled(disabled);
        if (ImGui.Begin("Better Mount Roulette", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            _wasOpen = true;
            _isEnabled = _plugin.Configuration.Enabled;
            _ = ImGui.Checkbox("Is enabled", ref _isEnabled);
            ImGui.Text("For this to take effect, you have to enable at least one mount.");
            _plugin.Configuration.Enabled = _isEnabled;

            bool enableNewMounts = _plugin.Configuration.IncludeNewMounts;
            _ = ImGui.Checkbox("Enable new mounts on unlock", ref enableNewMounts);
            if (enableNewMounts != _plugin.Configuration.IncludeNewMounts)
            {
                _plugin.Configuration.IncludeNewMounts = enableNewMounts;
                Mounts.Instance.UpdateUnlocked(enableNewMounts);
            }

            var pages = Mounts.Instance.PageCount;
            if (pages == 0)
            {
                ImGui.Text("Please unlock at least one mount.");
            }
            else if (ImGui.BeginTabBar("mount_pages"))
            {
                for (int page = 1; page <= pages; page++)
                {
                    if (ImGui.BeginTabItem($"{page}##mount_tab_{page}"))
                    {
                        Mounts.Instance.RenderItems(page);

                        var currentPage = page;
                        (bool Select, int? Page)? maybeInfo =
                            Buttons("Select all", "Unselect all", "Select page", "Unselect page") switch
                            {
                                0 => (true, default(int?)),
                                1 => (false, default(int?)),
                                2 => (true, page),
                                3 => (false, page),
                                _ => default((bool, int?)?),
                            };

                        if (maybeInfo is { } info)
                        {
                            string selectText = info.Select ? "select" : "unselect";
                            string pageInfo = (info.Page, info.Select) switch
                            {
                                (null, true) => "currently unselected mounts",
                                (null, false) => "currently selected mounts",
                                _ => "mounts on the current page",
                            };
                            RequestConfirmation(
                                $"Do you really want to {selectText}select all {pageInfo}?",
                                () => Mounts.Instance.Update(info.Select, info.Page));
                        }

                        ImGui.EndTabItem();
                    }
                }

                ImGui.SameLine();

                ImGui.EndTabBar();
            }
        }

        ImGui.End();
        ImGui.EndDisabled();

        ConfirmDialog();
    }

    private static int? Buttons(params string[] buttons)
    {
        int? result = null;
        for (int i = 0; i < buttons.Length; ++i)
        {
            if (i > 0)
            {
                ImGui.SameLine();
            }

            if (ImGui.Button(buttons[i]))
            {
                result = i;
            }
        }

        return result;
    }
}
