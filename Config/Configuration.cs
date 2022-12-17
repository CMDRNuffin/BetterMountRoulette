namespace BetterMountRoulette.Config;

using Dalamud.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;

internal class Configuration : IPluginConfiguration
{
    private const int CONFIG_VERSION = 2;

    public int Version { get; set; }

    public bool Enabled { get; set; }

    public string DefaultGroupName { get; set; } = "Default";

    public bool IncludeNewMounts { get; set; } = true;

    public List<uint> EnabledMounts { get; set; } = new();

    public List<MountGroup> Groups { get; set; } = new();

    public string? MountRouletteGroup { get; set; }

    public string? FlyingMountRouletteGroup { get; set; }

    public static Configuration Init()
    {
        var res = new Configuration { Version = CONFIG_VERSION };
        Mounts.Load(res);
        Mounts.GetInstance(res.DefaultGroupName)!.Update(res.IncludeNewMounts);
        Mounts.Remove(res.DefaultGroupName);
        return res;
    }

    public MountGroup? GetMountGroup(string name)
    {
        if (name == DefaultGroupName)
        {
            return new DefaultMountGroup(this);
        }

        return Groups.FirstOrDefault(x => x.Name == name);
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

        // insert migration code here

        if (Version < CONFIG_VERSION)
        {
            throw new InvalidOperationException($"Missing migration to version {CONFIG_VERSION}");
        }
    }
}
