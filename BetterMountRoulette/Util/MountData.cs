namespace BetterMountRoulette.Util;

using BetterRouletteBase.Util;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

internal sealed class MountData(ITextureProvider textureProvider, ReadOnlySeString name) : ItemData(textureProvider, name)
{
    public int ExtraSeats { get; set; }

    public bool IsFast { get; set; }

    public override unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Mount, ID, checkRecastActive: false, checkCastingActive: false) == 0;
    }
}
