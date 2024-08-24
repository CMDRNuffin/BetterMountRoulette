namespace BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

using System;
using Lumina.Excel.GeneratedSheets2;
using Lumina.Excel;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Iced.Intel;

internal sealed class GameFunctions : IDisposable
{
    private readonly Services _services;
    private readonly Dictionary<ushort, uint[]?> _maxSpeedUnlockCache = new();

    private bool _disposedValue;

    private readonly unsafe uint* _mountGuideRouletteIDs;

    public unsafe GameFunctions(Services services)
    {
        _services = services;
        services.GameInteropProvider.InitializeFromAttributes(this);

        _mountGuideRouletteIDs = (uint*)FindMountRouletteActionIDsTable();
        SetSecondaryMountRouletteActionId(24);
    }

    public unsafe bool HasMountUnlocked(uint id)
    {
        return PlayerState.Instance()->IsMountUnlocked(id);
    }

    private unsafe void SetSecondaryMountRouletteActionId(uint actionId)
    {
        if (_mountGuideRouletteIDs == null)
        {
            return;
        }

        MemoryHelper.ChangePermission((nint)_mountGuideRouletteIDs, 8, MemoryProtection.ReadWrite, out MemoryProtection oldPermission);
        _mountGuideRouletteIDs[1] = actionId;
        _ = MemoryHelper.ChangePermission((nint)_mountGuideRouletteIDs, 8, oldPermission);
    }

    private static unsafe nint FindMountRouletteActionIDsTable(int offset = 0)
    {
        nint vtable = (nint)AgentModule.Instance()->GetAgentByInternalId(AgentId.MountNotebook)->VirtualTable;
        byte* func = *((byte**)vtable + 24);
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

    public unsafe bool IsFlightUnlocked()
    {
        ExcelSheet<TerritoryType>? territory = _services.DataManager.GetExcelSheet<TerritoryType>();
        TerritoryType? currentTerritory = territory?.GetRow(_services.ClientState.TerritoryType);
        if (currentTerritory is not null)
        {
            if (currentTerritory.Unknown4 > 0)
            {
                return PlayerState.Instance()->IsAetherCurrentZoneComplete(currentTerritory.Unknown4);
            }
        }

        return false;
    }

    public unsafe (byte MaxSpeed, byte CurrentSpeed) GetCurrentTerritoryMountSpeedInfo()
    {
        uint[]? maxSpeedUnlockIds;
        if(!_maxSpeedUnlockCache.TryGetValue(_services.ClientState.TerritoryType, out maxSpeedUnlockIds))
        {
            TerritoryType? territoryType = _services.DataManager.GetExcelSheet<TerritoryType>()
                ?.GetRow(_services.ClientState.TerritoryType);
            if (territoryType != null)
            {
                if (territoryType.MountSpeed.Row > 0 && territoryType.MountSpeed.Value is { } mountSpeed)
                {
                    maxSpeedUnlockIds = (mountSpeed.Quest.Row, mountSpeed.Unknown0) switch
                    {
                        (0, _) => null,
                        (uint i, 0) => [i],
                        (uint i, uint j) => [i, j]
                    };
                }
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

    private unsafe void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            if (_mountGuideRouletteIDs != null)
            {
                SetSecondaryMountRouletteActionId(0);
            }

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

    private sealed unsafe class UnsafeCodeReader : CodeReader
    {
        private bool _hasEncounteredCC;
        private readonly byte* _address;
        private int _pos;

        public UnsafeCodeReader(byte* address)
        {
            _address = address;
        }

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
}
