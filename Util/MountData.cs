namespace BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text;

internal sealed class MountData
{
    private readonly TextureHelper _textureHelper;

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public SeString Name { get; }

    public bool Unlocked { get; set; }

    public int ExtraSeats { get; set; }

    public bool IsFast { get; set; }

    public MountData(TextureHelper textureHelper, SeString name)
    {
        _textureHelper = textureHelper;
        Name = name;
    }

    public nint GetIcon()
    {
        return _textureHelper.LoadIconTexture(IconID);
    }

    public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Mount, ID) == 0;
    }
}
