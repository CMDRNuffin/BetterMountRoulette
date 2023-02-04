namespace BetterMountRoulette.Config;

using System.Collections.Generic;
using System.Linq;

internal class ConfigurationBase
{
    public string DefaultGroupName { get; set; } = "Default";

    public bool IncludeNewMounts { get; set; } = true;

    public List<uint> EnabledMounts { get; set; } = new();

    public List<MountGroup> Groups { get; set; } = new();

    public string? MountRouletteGroup { get; set; }

    public string? FlyingMountRouletteGroup { get; set; }

    public void CopyFrom(ConfigurationBase other)
    {
        other.DefaultGroupName = DefaultGroupName;
        other.IncludeNewMounts = IncludeNewMounts;
        other.EnabledMounts = EnabledMounts;
        other.Groups = Groups;
        other.MountRouletteGroup = MountRouletteGroup;
        other.FlyingMountRouletteGroup = FlyingMountRouletteGroup;
    }

    public MountGroup? GetMountGroup(string name)
    {
        if (name == DefaultGroupName)
        {
            return new DefaultMountGroup(this);
        }

        return Groups.FirstOrDefault(x => x.Name == name);
    }
}
