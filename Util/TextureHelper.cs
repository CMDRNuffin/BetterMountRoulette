namespace BetterMountRoulette.Util;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class TextureHelper
{
    private readonly Dictionary<string, IDalamudTextureWrap> _loadedTextures = new();
    private readonly Dictionary<uint, IDalamudTextureWrap> _loadedIconTextures = new();
    private readonly Services _services;
    private ITextureProvider TextureProvider => _services.TextureProvider;

    public TextureHelper(Services services)
    {
        _services = services;
    }

    public nint LoadUldTexture(string name)
    {
        string path = $"ui/uld/{name}_hr1.tex";
        return LoadTexture(_loadedTextures, path, x => TextureProvider.GetTextureFromGame(x));
    }

    public nint LoadIconTexture(uint id)
    {
        return LoadTexture(_loadedIconTextures, id, x => TextureProvider.GetIcon(x));
    }

    public void Dispose()
    {
        var values = _loadedTextures.Values.Concat(_loadedIconTextures.Values).ToList();
        _loadedTextures.Clear();
        _loadedIconTextures.Clear();
        values.ForEach(x => x.Dispose());
    }

    private static nint LoadTexture<TKey>(
        Dictionary<TKey, IDalamudTextureWrap> cache,
        TKey key,
        Func<TKey, IDalamudTextureWrap?> loadFunc)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out IDalamudTextureWrap? texture))
        {
            try
            {
                return texture.ImGuiHandle;
            }
            catch (ObjectDisposedException)
            {
            }
        }

        texture = loadFunc(key);
        if (texture is null)
        {
            return nint.Zero;
        }

        cache[key] = texture;
        return texture.ImGuiHandle;
    }
}
