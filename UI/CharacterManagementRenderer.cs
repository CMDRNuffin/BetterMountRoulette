namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;
using ImGuiNET;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

internal sealed class CharacterManagementRenderer
{
    private readonly Services _services;
    private readonly WindowManager _windowManager;
    private readonly CharacterManager _characterManager;
    private readonly Configuration _configuration;
    private ulong? _currentCharacter;

    public CharacterManagementRenderer(
        Services services,
        WindowManager windowManager,
        CharacterManager characterManager,
        Configuration configuration)
    {
        _services = services;
        _windowManager = windowManager;
        _characterManager = characterManager;
        _configuration = configuration;
    }

    public void Draw()
    {
        RenderNewCharacterHandling();

        ImGui.Text("Existing characters");
        if (!ImGui.BeginListBox("##Characters"))
        {
            return;
        }

        string? selectedCharacterName = null;
        foreach (KeyValuePair<ulong, CharacterConfigEntry> character in _configuration.CharacterConfigs.OrderBy(x => x.Key))
        {
            StringBuilder sb = new(character.Value.CharacterName);
            if (!string.IsNullOrWhiteSpace(character.Value.CharacterWorld))
            {
                _ = sb.Append(CultureInfo.CurrentCulture, $" ({character.Value.CharacterWorld})");
            }

            string text = sb.ToString();

            if (ImGui.Selectable(text, _currentCharacter == character.Key))
            {
                Util.Toggle(ref _currentCharacter, character.Key);
            }

            if (_currentCharacter == character.Key)
            {
                selectedCharacterName = text;
            }
        }

        ImGui.EndListBox();
        ImGui.BeginDisabled(_currentCharacter is null || _currentCharacter == _services.ClientState.LocalContentId);

        if (ImGui.Button("Import"))
        {
            Debug.Assert(_currentCharacter is not null);
            ulong currentCharacter = _currentCharacter.Value;
            _windowManager.Confirm(
                "Import settings?",
                $"Import settings from {selectedCharacterName}? This will overwrite all settings for this character!",
                ("Confirm", () => ImportFromCharacter(currentCharacter)),
                "Cancel");
        }

        ImGui.SameLine();

        ImGui.BeginDisabled(_currentCharacter == Configuration.DUMMY_LEGACY_CONFIG_ID);
        if (ImGui.Button("Delete"))
        {
            Debug.Assert(_currentCharacter is not null);
            ulong currentCharacter = _currentCharacter.Value;
            _windowManager.Confirm(
                "Delete settings?",
                $"Delete settings for {selectedCharacterName}? This action cannot be undone!",
                ("Confirm", () => DeleteCharacter(currentCharacter)),
                "Cancel");
        }

        if (_currentCharacter == Configuration.DUMMY_LEGACY_CONFIG_ID)
        {
            ImGui.SameLine();
            ImGui.Text("This configuration cannot be deleted.");
        }
        else if (_currentCharacter == _services.ClientState.LocalContentId)
        {
            ImGui.SameLine();
            ImGui.Text("You cannot import from or delete the currently active character.");
        }

        ImGui.EndDisabled();
        ImGui.EndDisabled();
    }

    private void RenderNewCharacterHandling()
    {
        const int ASK = Configuration.NewCharacterHandlingModes.ASK;
        const int BLANK = Configuration.NewCharacterHandlingModes.BLANK;
        const int IMPORT = Configuration.NewCharacterHandlingModes.IMPORT;

        if (_configuration.CharacterConfigs.ContainsKey(Configuration.DUMMY_LEGACY_CONFIG_ID))
        {
            string text = "New characters: ";
            Vector2 offset = ImGui.CalcTextSize(text);
            float posX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(posX + offset.X);

            string characterHandlingMode = GetCharacterHandlingModeText(_configuration.NewCharacterHandling);

            if (ImGui.BeginCombo("##NewCharacterHandling", characterHandlingMode))
            {
                int? newCharacterHandling = _configuration.NewCharacterHandling;
                DrawSelection(ASK, ref newCharacterHandling);
                DrawSelection(BLANK, ref newCharacterHandling);
                DrawSelection(IMPORT, ref newCharacterHandling);
                _configuration.NewCharacterHandling = newCharacterHandling;

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(posX);
            ImGui.Text(text);

            ImGui.Text("Modes:");
            ImGui.Text($"• {GetCharacterHandlingModeText(IMPORT)}: For new characters, import legacy data on first login.");
            ImGui.Text($"• {GetCharacterHandlingModeText(BLANK)}: For new characters, create empty settings profile.");
            ImGui.Text($"• {GetCharacterHandlingModeText(ASK)}: Ask whether to import or not for each character individually.");
            ImGui.Separator();
        }

        static void DrawSelection(int mode, ref int? selectedMode)
        {
            if (ImGui.Selectable(GetCharacterHandlingModeText(mode), mode == selectedMode))
            {
                selectedMode = mode;
            }
        }

        static string GetCharacterHandlingModeText(int? characterHandlingMode)
        {
            return characterHandlingMode switch
            {
                ASK => "Ask",
                BLANK => "Create empty profile",
                IMPORT => "Import legacy data",
                _ => "Ask",
            };
        }
    }

    private void ImportFromCharacter(ulong characterID)
    {
        if (_characterManager.Import(characterID))
        {
            _windowManager.Confirm("Import", "Import successful!", "OK");
        }
        else
        {
            _windowManager.Confirm("Import", "Import failed: Unable to access character config.", "OK");
        }
    }

    private void DeleteCharacter(ulong characterID)
    {
        if (_configuration.CharacterConfigs.TryGetValue(characterID, out CharacterConfigEntry? cce))
        {
            _ = _configuration.CharacterConfigs.Remove(characterID);
            if (cce is not null && characterID is not Configuration.DUMMY_LEGACY_CONFIG_ID)
            {
                try
                {
                    File.Delete(Path.Combine(_services.DalamudPluginInterface.GetPluginConfigDirectory(), cce.FileName));
                }
                catch (IOException)
                {
                }
            }

            _services.DalamudPluginInterface.SavePluginConfig(_configuration);
        }
    }
}
