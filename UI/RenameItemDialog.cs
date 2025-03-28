namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;

using Dalamud.Interface.Colors;

using ImGuiNET;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

internal sealed class RenameItemDialog(string title, string initialName, Action<string> onComplete)
    : DialogWindow(title, ImGuiWindowFlags.AlwaysAutoResize)
{
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

    public override void Draw()
    {
        ImGui.Text("Name:");
        ImGui.SameLine();
        _ = ImGui.InputText("", ref _name, 1000);
        bool nameIsInvalid = !ValidateNameImpl();

        ImGui.BeginDisabled(nameIsInvalid);
        if (ImGui.Button("Save"))
        {
            _onComplete(_name);
            IsOpen = false;
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            IsOpen = false;
        }

        if (nameIsInvalid)
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, GetValidationErrorsImpl());
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
