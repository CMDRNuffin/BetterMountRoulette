namespace BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

using System.Globalization;

internal sealed class MountData(TextureHelper textureHelper, ReadOnlySeString name)
{
    private readonly TextureHelper _textureHelper = textureHelper;
    private readonly ReadOnlySeString _internalName = name;

    public uint ID { get; init; }

    public uint IconID { get; init; }

    public string Name => field ??= _internalName.ExtractText();

    public bool Unlocked { get; set; }

    public int ExtraSeats { get; set; }

    public bool IsFast { get; set; }

    public string CapitalizedName => field ??= CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Name);

    public ImTextureID GetIcon()
    {
        return _textureHelper.LoadIconTexture(IconID);
    }

    public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
    {
        return actionManager.Value->GetActionStatus(ActionType.Mount, ID) == 0;
    }
}
