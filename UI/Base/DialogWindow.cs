namespace BetterMountRoulette.UI.Base;

using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

internal abstract class DialogWindow : Window
{
    protected DialogWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {
        AllowClickthrough = false;
        AllowPinning = false;
    }

    public new bool ShowCloseButton
    {
        get => base.ShowCloseButton;
        set
        {
            base.ShowCloseButton = value;
            base.RespectCloseHotkey &= value;
        }
    }

    public new bool RespectCloseHotkey
    {
        get => base.RespectCloseHotkey;
        set
        {
            base.RespectCloseHotkey = value;
            base.ShowCloseButton |= value;
        }
    }

    public bool IsEnabled { get; set; } = true;

    public override void PreDraw()
    {
        base.PreDraw();
        ImGui.BeginDisabled(!IsEnabled);
    }

    public override void PostDraw()
    {
        ImGui.EndDisabled();
        base.PostDraw();
    }
}
