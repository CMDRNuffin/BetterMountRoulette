namespace BetterMountRoulette.UI;

using Dalamud.Interface.Colors;

using ImGuiNET;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

internal sealed class RenameItemDialog(
    WindowManager manager,
    string title,
    string initialName,
    Action<string> onComplete) : IWindow
{
    private readonly string _title = title;
    private readonly WindowManager _manager = manager;
    private string _name = initialName;
    private readonly Action<string> _onComplete = onComplete;
    private Func<string, bool>? _validateName;
    private Func<string, string>? _getValidationErrors;
    private static Regex? _normalizeWhitespaceRegex;

    public bool AllowEmptyName { get; set; }
    public bool NormalizeWhitespace { get; set; }

    public void SetValidation(Func<string, bool> validate, Func<string, string> getValidationErrors)
    {
        _validateName = validate;
        _getValidationErrors = getValidationErrors;
    }

    public void Draw()
    {
        bool isOpen = true;
        bool save = false;
        bool cancel = false;
        if (ImGui.Begin(_title, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Name:");
            ImGui.SameLine();
            _ = ImGui.InputText("", ref _name, 1000);
            bool nameIsInvalid = !ValidateNameImpl();

            ImGui.BeginDisabled(nameIsInvalid);
            save = ImGui.Button("Save");
            ImGui.EndDisabled();

            ImGui.SameLine();
            cancel = ImGui.Button("Cancel");

            if (nameIsInvalid)
            {
                ImGui.TextColored(ImGuiColors.DalamudOrange, GetValidationErrorsImpl());
            }
        }

        ImGui.End();

        if (save)
        {
            cancel = true;
            _onComplete(_name);
        }

        if (cancel)
        {
            isOpen = false;
        }

        if (!isOpen)
        {
            _manager.Close(this);
        }
    }

    private string GetValidationErrorsImpl()
    {
        Debug.Assert(!ValidateNameImpl(), "GetValidationErrors should only be called if validation failed");

        string name = GetNormalizedName();
        return !AllowEmptyName && string.IsNullOrEmpty(name)
            ? "Please provide a name."
            : _getValidationErrors is { } getValidationErrors
            ? getValidationErrors(name)
            : "Unknown validation error.";
    }

    private bool ValidateNameImpl()
    {
        string name = GetNormalizedName();
        return (AllowEmptyName || !string.IsNullOrEmpty(name))
            && (_validateName is not { } validateName || validateName(name));
    }

    private string GetNormalizedName()
    {
        return NormalizeWhitespace ? NormalizeWhiteSpace(_name) : _name;
    }

    public static string NormalizeWhiteSpace(string value)
    {
        _normalizeWhitespaceRegex ??= new Regex(@"\s+", RegexOptions.Compiled);
        return _normalizeWhitespaceRegex.Replace(value, " ").Trim();
    }
}
