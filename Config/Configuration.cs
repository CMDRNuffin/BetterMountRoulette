namespace BetterMountRoulette.Config;

using Dalamud.Configuration;

using System;
using System.Collections.Generic;

internal sealed class Configuration : ConfigurationBase, IPluginConfiguration
{
    private const int CONFIG_VERSION = 3;
    private const ulong DUMMY_LEGACY_CONFIG_ID = 0;

    public int Version { get; set; }

    public bool Enabled { get; set; }

    public List<CharacterConfig> CharacterConfigs { get; set; } = new();

    public static Configuration Init()
    {
        var res = new Configuration { Version = CONFIG_VERSION };
        Mounts.Load(res);
        Mounts.GetInstance(res.DefaultGroupName)!.Update(res.IncludeNewMounts);
        Mounts.Remove(res.DefaultGroupName);
        return res;
    }

    public void Migrate()
    {
        if (Version <= 1)
        {
            Version = 2;

            string? defaultGroup = Enabled ? DefaultGroupName : null;
            FlyingMountRouletteGroup = defaultGroup;
            MountRouletteGroup = defaultGroup;
        }

        if (Version <= 2)
        {
            Version = 3;
            CharacterConfigs.Add(new CharacterConfig { CharacterID = DUMMY_LEGACY_CONFIG_ID, CharacterName = "Legacy Data" });
        }
        // insert migration code here

        if (Version < CONFIG_VERSION)
        {
            throw new InvalidOperationException($"Missing migration to version {CONFIG_VERSION}");
        }
    }
}
