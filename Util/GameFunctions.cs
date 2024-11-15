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
using FFXIVClientStructs.FFXIV.Component.Exd;

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
    private readonly Services _services;
    private readonly Dictionary<ushort, uint[]?> _maxSpeedUnlockCache = [];

    private bool _disposedValue;

    private readonly Hook<ExdModule.Delegates.GetRowBySheetIndexAndRowIndex> _exdModuleGetRowBySheetIndexAndRowIndexHook;

    [Signature("E8 ?? ?? ?? ?? 0F B6 54 24 40 49 8B CD", Fallibility = Fallibility.Infallible)]
    private readonly unsafe delegate* unmanaged<AgentActionMenu*, bool, void> _agentActionMenuLoadActions;

    [Signature("E8 ?? ?? ?? ?? 41 83 FF 04 0F 84 ?? ?? ?? ??", Fallibility = Fallibility.Infallible)]
    private readonly Hook<ActionManagerInitializeCastBarDetour> _actionManagerInitializeCastbarHook;

    private readonly AgentMountNoteBookHooks _agentMountNoteBookHooks;

    private unsafe delegate void AgentActionMenuLoadActionsDetour(AgentActionMenu* @this, bool reload);
    private unsafe delegate void ActionManagerInitializeCastBarDetour(ActionManager* @this, BattleChara* chara, ActionType actionType, uint actionId, uint spellId, int mountRouletteIndex, float castTimeElapsed);

    public unsafe GameFunctions(Services services)
    {
        _services = services;
        _agentActionMenuLoadActions = null;
        _actionManagerInitializeCastbarHook = null!;
        services.GameInteropProvider.InitializeFromAttributes(this);

        _actionManagerInitializeCastbarHook.Enable();

        _exdModuleGetRowBySheetIndexAndRowIndexHook = _services.GameInteropProvider.HookFromAddress<ExdModule.Delegates.GetRowBySheetIndexAndRowIndex>(
            ExdModule.MemberFunctionPointers.GetRowBySheetIndexAndRowIndex,
            ExdModuleGetRowBySheetIndexAndRowIndexHandler);
        _exdModuleGetRowBySheetIndexAndRowIndexHook.Enable();

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
        return currentTerritory.Unknown4 > 0
            && PlayerState.Instance()->IsAetherCurrentZoneComplete(currentTerritory.Unknown4);
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

    private unsafe void OnActionManagerInitializeCastBar(ActionManager* @this, BattleChara* chara, ActionType actionType, uint actionId, uint spellId, int mountRouletteIndex, float castTimeElapsed)
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

        _actionManagerInitializeCastbarHook.Original(@this, chara, actionType, actionId, spellId, mountRouletteIndex, castTimeElapsed);
    }

    private unsafe CSExcelRow* ExdModuleGetRowBySheetIndexAndRowIndexHandler(ExdModule* thisPtr, uint sheetIndex, uint rowIndex)
    {
        CSExcelRow* row = _exdModuleGetRowBySheetIndexAndRowIndexHook.Original(thisPtr, sheetIndex, rowIndex);
        if (sheetIndex == 0x68 /* General Action */ && rowIndex == 24 /* flying mount roulette */)
        {
            new FlyingMountRoulette(row).EnableUIPriority();
            _exdModuleGetRowBySheetIndexAndRowIndexHook.Disable();
            _services.PluginLog.Debug("Hooked flying mount roulette");
        }

        return row;
    }

    private unsafe void UpdateActionMenu()
    {
        if (_services.ClientState.IsLoggedIn)
        {
            AgentActionMenu* agentActionMenu = AgentModule.Instance()->GetAgentActionMenu();
            if (agentActionMenu->GeneralList.Count > 0)
            {
                // refresh action menu to add the flying mount roulette
                _agentActionMenuLoadActions(AgentModule.Instance()->GetAgentActionMenu(), true);
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

            _exdModuleGetRowBySheetIndexAndRowIndexHook.Dispose();

            var rouletteItem = new FlyingMountRoulette(Framework.Instance()->ExdModule->GetRowBySheetIndexAndRowIndex(0x68, 24));
            rouletteItem.DisableUIPriority();

            UpdateActionMenu();

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

        public void EnableUIPriority()
        {
            if (_sheet != null)
            {
                ((byte*)_sheet->Data)[UI_PRIORITY_OFFSET] = UI_PRIORITY_VALUE;
            }
        }

        public void DisableUIPriority()
        {
            if (_sheet != null)
            {
                ((byte*)_sheet->Data)[UI_PRIORITY_OFFSET] = 0;
            }
        }

        public byte UIPriority => _sheet != null ? ((byte*)_sheet->Data)[UI_PRIORITY_OFFSET] : (byte)0;
    }
}
