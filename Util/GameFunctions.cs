namespace BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

using System;
using Lumina.Excel.GeneratedSheets2;
using Lumina.Excel;

internal sealed class GameFunctions : IDisposable
{
    private readonly Services _services;

    private bool _disposedValue;

    public unsafe GameFunctions(Services services)
    {
        _services = services;
        services.GameInteropProvider.InitializeFromAttributes(this);
    }

    public unsafe bool HasMountUnlocked(uint id)
    {
        return PlayerState.Instance()->IsMountUnlocked(id);
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
        TerritoryType? territoryType = _services.DataManager.GetExcelSheet<TerritoryType>()
            ?.GetRow(_services.ClientState.TerritoryType);
        if (territoryType == null)
        {
            return (0, 0);
        }

        if (territoryType.MountSpeed.Row > 0 && territoryType.MountSpeed.Value is { } mountSpeed)
        {
            byte maxSpeed = 0;
            byte currentSpeed = 0;

            uint[] unlockIds = [mountSpeed.Quest.Row, mountSpeed.Unknown0];
            foreach (uint unlockId in unlockIds)
            {
                if (unlockId == 0)
                {
                    break;
                }

                maxSpeed += 1;

                if (UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockId))
                {
                    currentSpeed += 1;
                }
            }

            return (maxSpeed, currentSpeed);
        }

        return (0, 0);
    }

    private unsafe void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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
}
