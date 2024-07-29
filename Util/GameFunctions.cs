namespace BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

using System;

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
