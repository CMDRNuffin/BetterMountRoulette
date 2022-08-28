namespace BetterMountRoulette.Util;

using Dalamud.Data;
using ImGuiScene;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class TextureHelper
{
    private static readonly Dictionary<string, TextureWrap> _loadedTextures = new();
    private static readonly Dictionary<uint, TextureWrap> _loadedIconTextures = new();

    private static DataManager DataManager => BetterMountRoulettePlugin.DataManager;

    public static IntPtr LoadUldTexture(string name)
    {
        string path = $"ui/uld/{name}_hr1.tex";
        return LoadTexture(_loadedTextures, path, DataManager.GetImGuiTexture);
    }

    public static IntPtr LoadIconTexture(uint id)
    {
        return LoadTexture(_loadedIconTextures, id, DataManager.GetImGuiTextureIcon);
    }

    public static void Dispose()
    {
        var values = _loadedTextures.Values.Concat(_loadedIconTextures.Values).ToList();
        _loadedTextures.Clear();
        _loadedIconTextures.Clear();
        values.ForEach(x => x.Dispose());
    }

    private static IntPtr LoadTexture<TKey>(
        Dictionary<TKey, TextureWrap> cache,
        TKey key,
        Func<TKey, TextureWrap?> loadFunc)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out TextureWrap? texture))
        {
            return texture.ImGuiHandle;
        }

        texture = loadFunc(key);
        if (texture is null)
        {
            return IntPtr.Zero;
        }

        cache[key] = texture;
        return texture.ImGuiHandle;
    }
}
