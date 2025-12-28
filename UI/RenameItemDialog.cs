namespace BetterMountRoulette.UI;

using BetterMountRoulette.UI.Base;

using Dalamud.Interface.Colors;

using Dalamud.Bindings.ImGui;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BetterMountRoulette.Util.Memory;

internal sealed class RenameItemDialog(string title, string initialName, Action<string> onComplete)
    : DialogWindow(title, ImGuiWindowFlags.AlwaysAutoResize)
{
    private string _name = initialName;
    private StringView _normalized = initialName;
    private readonly Action<string> _onComplete = onComplete;
    private Func<StringView, bool>? _validateName;
    private Func<StringView, ReadOnlySpan<byte>>? _getValidationErrors;
    private static Regex? _normalizeWhitespaceRegex;

    public bool AllowEmptyName { get; set; }
    public bool NormalizeWhitespace { get; set; }

    public void SetValidation(Func<StringView, bool> validate, Func<StringView, ReadOnlySpan<byte>> getValidationErrors)
    {
        _validateName = validate;
        _getValidationErrors = getValidationErrors;
    }

    public override void Draw()
    {
        ImGui.Text("Name:"u8);
        ImGui.SameLine();
        if (ImGui.InputText(""u8, ref _name, 1000))
        {
            _normalized = GetNormalizedName();
        }

        bool nameIsInvalid = !ValidateNameImpl();

        ImGui.BeginDisabled(nameIsInvalid);
        if (ImGui.Button("Save"u8))
        {
            _onComplete(_name);
            IsOpen = false;
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8))
        {
            IsOpen = false;
        }

        if (nameIsInvalid)
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, GetValidationErrorsImpl());
        }
    }

    private ReadOnlySpan<byte> GetValidationErrorsImpl()
    {
        Debug.Assert(!ValidateNameImpl(), "GetValidationErrors should only be called if validation failed");

        StringView name = GetNormalizedName();
        return !AllowEmptyName && name.IsEmpty
            ? "Please provide a name."u8
            : _getValidationErrors is { } getValidationErrors
            ? getValidationErrors(name)
            : "Unknown validation error."u8;
    }

    private bool ValidateNameImpl()
    {
        return (AllowEmptyName || !_normalized.IsEmpty)
            && (_validateName is not { } validateName || validateName(_normalized));
    }

    private StringView GetNormalizedName()
    {
        return NormalizeWhitespace ? NormalizeWhiteSpace(_name) : _name;
    }

    public static StringView NormalizeWhiteSpace(string value)
    {
        _normalizeWhitespaceRegex ??= new Regex(@"\s+", RegexOptions.Compiled);
        return new StringView(_normalizeWhitespaceRegex.Replace(value, " ")).Trim();
    }
}
