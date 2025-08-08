namespace BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

internal sealed class MountData(TextureHelper textureHelper, ReadOnlySeString name)
{
    private readonly TextureHelper _textureHelper = textureHelper;

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public ReadOnlySeString Name { get; } = name;

    public bool Unlocked { get; set; }

    public int ExtraSeats { get; set; }

    public bool IsFast { get; set; }

    public ImTextureID GetIcon()
    {
        return _textureHelper.LoadIconTexture(IconID);
    }

    public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Mount, ID) == 0;
    }
}
