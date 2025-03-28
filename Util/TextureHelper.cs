﻿namespace BetterMountRoulette.Util;

using Dalamud.Plugin.Services;

internal sealed class TextureHelper(PluginServices services)
{
    private readonly PluginServices _services = services;
    private ITextureProvider TextureProvider => _services.TextureProvider;

    public nint LoadUldTexture(string name)
    {
        string path = $"ui/uld/{name}_hr1.tex";
        return TextureProvider.GetFromGame(path).GetWrapOrEmpty().ImGuiHandle;
    }

    public nint LoadIconTexture(uint id)
    {
        return TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(id)).GetWrapOrEmpty().ImGuiHandle;
    }
}
