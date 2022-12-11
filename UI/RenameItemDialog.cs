namespace BetterMountRoulette.UI;

using Dalamud.Interface.Colors;

using ImGuiNET;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

internal class RenameItemDialog : IWindow
{
    private readonly string _title;
    private readonly WindowManager _manager;
    private string _name;
    private readonly Action<string> _onComplete;
    private Func<string, bool>? _validateName;
    private Func<string, string>? _getValidationErrors;
    private static Regex? _normalizeWhitespaceRegex;

    public RenameItemDialog(
        WindowManager manager,
        string title,
        string initialName,
        Action<string> onComplete)
    {
        _title = title;
        _name = initialName;
        _onComplete = onComplete;
        _manager = manager;
    }

    public bool AllowEmptyName { get; set; } = false;
    public bool NormalizeWhitespace { get; set; } = false;

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
            var nameIsInvalid = !ValidateNameImpl();

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

        var name = GetNormalizedName();
        if (!AllowEmptyName && string.IsNullOrEmpty(name))
        {
            return "Please provide a name.";
        }

        return _getValidationErrors is { } getValidationErrors
            ? getValidationErrors(name)
            : "Unknown validation error.";
    }

    private bool ValidateNameImpl()
    {
        var name = GetNormalizedName();
        if (!AllowEmptyName && string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _validateName is not { } validateName || validateName(name);
    }

    private string GetNormalizedName()
    {
        return NormalizeWhitespace ? NormalizeWhiteSpace(_name) : _name;
    }

    public static string NormalizeWhiteSpace(string value)
    {
        _normalizeWhitespaceRegex ??= new Regex(@"\s+", RegexOptions.Compiled);
        return _normalizeWhitespaceRegex.Replace(value, " ").Trim(); ;
    }
}
