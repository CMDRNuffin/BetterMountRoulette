namespace BetterMountRoulette.Util;

using BetterRouletteBase.Util;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

internal sealed class MountData : ItemData
{
    public int ExtraSeats { get; set; }

    public bool IsFast { get; set; }

    public MountData(ITextureProvider textureProvider, ReadOnlySeString name) : base(textureProvider, name)
    {
    }

    public override unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Mount, ID, checkRecastActive: false, checkCastingActive: false) == 0;
    }
}
