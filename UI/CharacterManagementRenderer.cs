namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

internal sealed class CharacterManagementRenderer(
    PluginServices services,
    WindowManager windowManager,
    CharacterManager characterManager,
    Configuration configuration)
{
    private readonly PluginServices _services = services;
    private readonly WindowManager _windowManager = windowManager;
    private readonly CharacterManager _characterManager = characterManager;
    private readonly Configuration _configuration = configuration;
    private ulong? _currentCharacter;

    public void Draw()
    {
        RenderNewCharacterHandling();

        ImGui.Text("Existing characters"u8);
        if (!ImGui.BeginListBox("##Characters"u8))
        {
            return;
        }

        ReadOnlySpan<byte> selectedCharacterName = null;
        foreach (KeyValuePair<ulong, CharacterConfigEntry> character in _configuration.CharacterConfigs.OrderBy(x => x.Key))
        {
            ReadOnlySpan<byte> text = StringCache.Characters[character.Key, () => FormatCharacter(character.Value)];

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
                $"Import settings from {Encoding.UTF8.GetString(selectedCharacterName)}? This will overwrite all settings for this character!",
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
                $"Delete settings for {Encoding.UTF8.GetString(selectedCharacterName)}? This action cannot be undone!",
                ("Confirm", () => DeleteCharacter(currentCharacter)),
                "Cancel");
        }

        if (_currentCharacter == Configuration.DUMMY_LEGACY_CONFIG_ID)
        {
            ImGui.SameLine();
            ImGui.Text("This configuration cannot be deleted."u8);
        }
        else if (_currentCharacter == _services.ClientState.LocalContentId)
        {
            ImGui.SameLine();
            ImGui.Text("You cannot import from or delete the currently active character."u8);
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
            ReadOnlySpan<byte> text = "New characters: "u8;
            Vector2 offset = ImGui.CalcTextSize(text);
            float posX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(posX + offset.X);

            ReadOnlySpan<byte> characterHandlingMode = GetCharacterHandlingModeText(_configuration.NewCharacterHandling);

            if (ImGui.BeginCombo("##NewCharacterHandling"u8, characterHandlingMode))
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
            ImGui.Text(StringCache.Named["NewCharacterImport", () => CharacterHandlingModeExplanation(IMPORT)]);
            ImGui.Text(StringCache.Named["NewCharacterBlank", () => CharacterHandlingModeExplanation(BLANK)]);
            ImGui.Text(StringCache.Named["NewCharacterAsk", () => CharacterHandlingModeExplanation(ASK)]);
            ImGui.Separator();
        }

        static void DrawSelection(int mode, ref int? selectedMode)
        {
            if (ImGui.Selectable(GetCharacterHandlingModeText(mode), mode == selectedMode))
            {
                selectedMode = mode;
            }
        }

        static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3)
        {
            byte[] res = new byte[part1.Length + part2.Length + part3.Length];
            part1.CopyTo(res.AsSpan());
            part2.CopyTo(res.AsSpan(part1.Length));
            part3.CopyTo(res.AsSpan(part1.Length + part2.Length));
            return res;
        }

        static byte[] CharacterHandlingModeExplanation(int? characterHandlingMode)
        {
            ReadOnlySpan<byte> part1 = "• "u8;
            ReadOnlySpan<byte> part2 = GetCharacterHandlingModeText(characterHandlingMode);
            ReadOnlySpan<byte> part3 = characterHandlingMode switch
            {
                BLANK => ": For new characters, create empty settings profile."u8,
                IMPORT => ": For new characters, import legacy data on first login."u8,
                ASK or _ => ": Ask whether to import or not for each character individually."u8,
            };

            return Concat(part1, part2, part3);
        } 

        static ReadOnlySpan<byte> GetCharacterHandlingModeText(int? characterHandlingMode)
        {
            return characterHandlingMode switch
            {
                ASK => "Ask"u8,
                BLANK => "Create empty profile"u8,
                IMPORT => "Import legacy data"u8,
                _ => "Ask"u8,
            };
        }
    }

    private static string FormatCharacter(CharacterConfigEntry entry)
    {
        StringBuilder sb = new(entry.CharacterName);
        if (!string.IsNullOrWhiteSpace(entry.CharacterWorld))
        {
            _ = sb.Append(CultureInfo.CurrentCulture, $" ({entry.CharacterWorld})");
        }

        return sb.ToString();
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
