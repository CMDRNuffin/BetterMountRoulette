namespace BetterMountRoulette.UI;

using BetterMountRoulette.Config;
using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using BetterRouletteBase.UI;
using BetterRouletteBase.Util;

using Dalamud.Bindings.ImGui;

using System;
using System.Numerics;

internal sealed class CharacterManagementRenderer(
    PluginServices services,
    WindowManager windowManager,
    CharacterManager characterManager,
    Configuration configuration
)
    : CharacterManagementRendererBase<Configuration>(
        services.PlayerState,
        services.DalamudPluginInterface,
        windowManager,
        characterManager,
        configuration
    )
{
    protected override bool IsPredefinedEntry(ulong? characterId)
    {
        return characterId == Configuration.DUMMY_LEGACY_CONFIG_ID;
    }

    protected override void PluginSpecificSettings(Configuration configuration)
    {
        const int ASK = Configuration.NewCharacterHandlingModes.ASK;
        const int BLANK = Configuration.NewCharacterHandlingModes.BLANK;
        const int IMPORT = Configuration.NewCharacterHandlingModes.IMPORT;

        if (configuration.CharacterConfigs.ContainsKey(Configuration.DUMMY_LEGACY_CONFIG_ID))
        {
            ReadOnlySpan<byte> text = "New characters: "u8;
            Vector2 offset = ImGui.CalcTextSize(text);
            float posX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(posX + offset.X);

            ReadOnlySpan<byte> characterHandlingMode = GetCharacterHandlingModeText(configuration.NewCharacterHandling);

            if (ImGui.BeginCombo("##NewCharacterHandling"u8, characterHandlingMode))
            {
                int? newCharacterHandling = configuration.NewCharacterHandling;
                DrawSelection(ASK, ref newCharacterHandling);
                DrawSelection(BLANK, ref newCharacterHandling);
                DrawSelection(IMPORT, ref newCharacterHandling);
                configuration.NewCharacterHandling = newCharacterHandling;

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
}
