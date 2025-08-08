namespace BetterMountRoulette.Util;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

internal sealed class TextureHelper(PluginServices services)
{
    private readonly PluginServices _services = services;
    private ITextureProvider TextureProvider => _services.TextureProvider;

    public ImTextureID LoadUldTexture(string name)
    {
        string path = $"ui/uld/{name}_hr1.tex";
        return TextureProvider.GetFromGame(path).GetWrapOrEmpty().Handle;
    }

    public ImTextureID LoadIconTexture(uint id)
    {
        return TextureProvider.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrEmpty().Handle;
    }
}
