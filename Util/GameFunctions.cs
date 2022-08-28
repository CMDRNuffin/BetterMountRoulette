namespace BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game.UI;

internal static class GameFunctions
{
    public static unsafe bool HasMountUnlocked(uint id)
    {
        return PlayerState.Instance()->IsMountUnlocked(id);
    }
}
