namespace BetterMountRoulette.Config;

using Dalamud.Configuration;

using System.Collections.Generic;

internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public bool Enabled { get; set; }

    public bool IncludeNewMounts { get; set; } = true;

    public List<uint> EnabledMounts { get; set; } = new();

    public static Configuration Init()
    {
        var res = new Configuration { Version = 1 };
        Mounts.Instance.Save(res);
        return res;
    }
}
