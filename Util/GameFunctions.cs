namespace BetterMountRoulette.Util;

using BetterMountRoulette.Util.Hooks;

using System;
using System.Collections.Generic;

using Lumina.Excel.Sheets;
using Lumina.Excel;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Iced.Intel;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using CSExcelRow = FFXIVClientStructs.FFXIV.Component.Excel.ExcelRow;

public enum MountRouletteOverride
{
    PlainMount = 0,
    NormalRoulette = 1,
    FlyingRoulette = 2,
}

internal sealed class GameFunctions : IDisposable
{
    private const uint GENERAL_ACTION_SHEET_INDEX = 0x68;
    private const uint FLYING_MOUNT_ROULETTE_ROW_INDEX = 24;

    private readonly PluginServices _services;
    private readonly Dictionary<ushort, uint[]?> _maxSpeedUnlockCache = [];

    private bool _disposedValue;

    // For finding it in the future:
    // for ( i = 1; i < 0x2B; ++i )
    // {
    //     GeneralActionRow_1 = Component::Exd::ExdModule_GetGeneralActionRow_1(i);
    [Signature("84 D2 0F 84 ?? ?? ?? ?? 4C 8B DC 41 56 48 81 EC 70 01 00 00", Fallibility = Fallibility.Infallible)]
    private readonly unsafe delegate* unmanaged<AgentActionMenu*, bool, void> _agentActionMenuLoadActions;

    // For finding it in the future:
    // ActionManager.UseAction -> ActionManager.UseActionLocation
    // -> sub_DEADBEEF(this, some_game_object, actionType, actionId, spellId, extraParam, .../* in call, not definition */)
    // sub_DEADBEEF: if(!inIdleCam && !inGPose && gameMain.instance.(some_field & 0x40000000 == 0 || byte4(some_other_field) >= 0x40u)) ...
    [Signature("E8 ?? ?? ?? ?? 41 83 FC 04 0F 84 ?? ?? ?? ?? 41 81 FC 5D 4E 00 00", Fallibility = Fallibility.Infallible)]
    private readonly Hook<ActionManagerInitializeCastBarDetour> _actionManagerInitializeCastbarHook;

    private readonly AgentMountNoteBookHooks _agentMountNoteBookHooks;

    private unsafe delegate void AgentActionMenuLoadActionsDetour(AgentActionMenu* @this, bool reload);
    private unsafe delegate void ActionManagerInitializeCastBarDetour(ActionManager* @this, BattleChara* chara, ActionType actionType, uint actionId, uint spellId, int mountRouletteIndex);

    public unsafe GameFunctions(PluginServices services)
    {
        _services = services;
        _agentActionMenuLoadActions = null;
        _actionManagerInitializeCastbarHook = null!;
        services.GameInteropProvider.InitializeFromAttributes(this);

        _actionManagerInitializeCastbarHook.Enable();

        UpdateActionMenu();

        _agentMountNoteBookHooks = new(services);
    }

    public MountRouletteOverride? NextMountRouletteOverride { get; set; }

    public unsafe bool HasMountUnlocked(uint id)
    {
        return PlayerState.Instance()->IsMountUnlocked(id);
    }

    public unsafe bool IsFlightUnlocked()
    {
        ExcelSheet<TerritoryType> territory = _services.DataManager.GetExcelSheet<TerritoryType>();
        TerritoryType currentTerritory = territory.GetRow(_services.ClientState.TerritoryType);
        return currentTerritory.AetherCurrentCompFlgSet.IsValid
            && PlayerState.Instance()->IsAetherCurrentZoneComplete(currentTerritory.AetherCurrentCompFlgSet.RowId);
    }

    public unsafe (byte MaxSpeed, byte CurrentSpeed) GetCurrentTerritoryMountSpeedInfo()
    {
        if (!_maxSpeedUnlockCache.TryGetValue(_services.ClientState.TerritoryType, out uint[]? maxSpeedUnlockIds))
        {
            TerritoryType territoryType = _services.DataManager.GetExcelSheet<TerritoryType>().GetRow(_services.ClientState.TerritoryType);
            if (territoryType.MountSpeed.IsValid)
            {
                MountSpeed mountSpeed = territoryType.MountSpeed.Value;
#pragma warning disable IDE0072 // Add missing cases // exhaustive, adding a default case would be an error
                maxSpeedUnlockIds = (mountSpeed.Quest.RowId, mountSpeed.Unknown0) switch
                {
                    (0, _) => null,
                    (uint i, 0) => [i],
                    (uint i, uint j) => [i, j]
                };
#pragma warning restore IDE0072 // Add missing cases
            }

            _maxSpeedUnlockCache[_services.ClientState.TerritoryType] = maxSpeedUnlockIds;
        }

        if (maxSpeedUnlockIds == null)
        {
            return (0, 0);
        }

        byte maxSpeed = 0;
        byte currentSpeed = 0;

        foreach (uint unlockId in maxSpeedUnlockIds)
        {
            maxSpeed += 1;

            if (UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockId))
            {
                currentSpeed += 1;
            }
        }

        return (maxSpeed, currentSpeed);
    }

    private static unsafe nint GetStaticAddressFromVFunc(nint vtable, int vfunc, int offset = 0)
    {
        // adapted from GetStaticAddressFromSig, see
        // https://github.com/goatcorp/Dalamud/blob/cddad72066ba45633896b81e38f478bce1aaf674/Dalamud/Game/SigScanner.cs
        byte* func = *((byte**)vtable + vfunc);
        try
        {
            var reader = new UnsafeCodeReader(func);
            var decoder = Decoder.Create(64, reader, (ulong)func, DecoderOptions.AMD);
            while (reader.CanReadByte)
            {
                Instruction instruction = decoder.Decode();
                if (instruction.IsInvalid)
                {
                    continue;
                }

                if (instruction.Op0Kind is OpKind.Memory || instruction.Op1Kind is OpKind.Memory)
                {
                    return (IntPtr)instruction.MemoryDisplacement64;
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            // ignored
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return nint.Zero;
    }

    private unsafe void OnActionManagerInitializeCastBar(ActionManager* @this, BattleChara* chara, ActionType actionType, uint actionId, uint spellId, int mountRouletteIndex)
    {
        if (chara == Control.GetLocalPlayer()) /* don't bork other players' cast bars */
        {
            // mount roulettes are normalized to action type = mount; action id = mount id; mount roulette index = 1 for normal, 2 for flying
            if (actionType == ActionType.Mount && NextMountRouletteOverride is { } rouletteOverride)
            {
                mountRouletteIndex = (int)rouletteOverride;
                NextMountRouletteOverride = null;
            }
        }

        _actionManagerInitializeCastbarHook.Original(@this, chara, actionType, actionId, spellId, mountRouletteIndex);
    }

    private unsafe void UpdateActionMenu()
    {
        if (_services.ClientState.IsLoggedIn)
        {
            AgentActionMenu* agentActionMenu = AgentModule.Instance()->GetAgentActionMenu();
            if (agentActionMenu->GeneralList.Count > 0)
            {
                // refresh action menu to add the flying mount roulette
                _agentActionMenuLoadActions(agentActionMenu, true);
            }
        }
    }

    private unsafe void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // discard the task. we don't care about waiting
            _ = _services.Framework.RunOnFrameworkThread(() => ToggleFlyingRouletteButton(false));

            _agentMountNoteBookHooks.Dispose();
            _actionManagerInitializeCastbarHook.Dispose();

            _disposedValue = true;
        }
    }

    ~GameFunctions()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Enables or disables the flying mount roulette action in the Actions &amp; Traits and Mount Guide windows.
    /// Requires being run on the framework thread to prevent racing conditions.
    /// </summary>
    /// <param name="enableFlyingRouletteButton">The desired display state of the flying mount roulette</param>
    /// <returns>A boolean indicating whether the change was successful</returns>
    internal unsafe bool ToggleFlyingRouletteButton(bool enableFlyingRouletteButton)
    {
        return _services.Framework.IsInFrameworkUpdateThread && ToggleFlyingRouletteButtonInternal(enableFlyingRouletteButton);
    }

    private unsafe bool ToggleFlyingRouletteButtonInternal(bool enableFlyingRouletteButton)
    {
        CSExcelRow* excelSheet = Framework.Instance()->ExdModule->GetRowBySheetIndexAndRowIndex(GENERAL_ACTION_SHEET_INDEX, FLYING_MOUNT_ROULETTE_ROW_INDEX);
        if (excelSheet is null || excelSheet->Data is null)
        {
            return false;
        }

        var rouletteItem = new FlyingMountRoulette(excelSheet);

        Action<AgentMountNoteBookHooks> toggleHooksAction;

        if (enableFlyingRouletteButton)
        {
            toggleHooksAction = x => x.Enable();
        }
        else
        {
            toggleHooksAction = x => x.Disable();
        }

        rouletteItem.SetValue(enableFlyingRouletteButton);
        toggleHooksAction(_agentMountNoteBookHooks);
        UpdateActionMenu();

        return true;
    }

    private sealed unsafe class UnsafeCodeReader(byte* address) : CodeReader
    {
        private bool _hasEncounteredCC;
        private readonly byte* _address = address;
        private int _pos;

        public bool CanReadByte => !_hasEncounteredCC;

        public override int ReadByte()
        {
            if (_hasEncounteredCC)
            {
                return -1;
            }

            byte res = *(_address + _pos++);
            if (res == 0xCC)
            {
                _hasEncounteredCC = true;
            }

            return res;
        }
    }

    private readonly unsafe ref struct FlyingMountRoulette(CSExcelRow* sheet)
    {
        private readonly CSExcelRow* _sheet = sheet;

        private const int UI_PRIORITY_OFFSET = 0x12;

        private const byte UI_PRIORITY_VALUE = 22 /* between mount roulette and minion roulette */;
        private const byte DISABLED = 0;

        public byte UIPriority => _sheet != null ? ((byte*)_sheet->Data)[UI_PRIORITY_OFFSET] : (byte)0;

        public void SetValue(bool value)
        {
            SetValue(_sheet, value ? UI_PRIORITY_VALUE : DISABLED);
        }

        private static void SetValue(CSExcelRow* row, byte value)
        {
            ((byte*)row->Data)[UI_PRIORITY_OFFSET] = value;
        }
    }
}
