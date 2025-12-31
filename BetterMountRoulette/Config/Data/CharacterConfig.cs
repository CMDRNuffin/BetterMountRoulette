namespace BetterMountRoulette.Config.Data;

using BetterRouletteBase.Config;
using BetterRouletteBase.Util.Memory;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class CharacterConfig : ICharacterConfig<MountGroup>
{
    [JsonIgnore]
    public bool IsNew { get; set; }

    public bool IncludeNewMounts { get; set; } = true;

    public List<MountGroup> Groups { get; set; } = [];

    public string? MountRouletteGroup { get; set; }

    public bool RevealMountsNormal { get; set; }

    public bool RevealMountsFlying { get; set; }

    public string? FlyingMountRouletteGroup { get; set; }

    public bool SuppressChatErrors { get; set; }

    public bool EnableFlyingRouletteButton { get; set; } = true;

    [JsonIgnore]
    public bool HasNonDefaultGroups => Groups.Count > 1;

    public void CopyFrom(CharacterConfig other)
    {
        IncludeNewMounts = other.IncludeNewMounts;
        Groups = other.Groups;
        MountRouletteGroup = other.MountRouletteGroup;
        FlyingMountRouletteGroup = other.FlyingMountRouletteGroup;
    }

    public MountGroup? GetGroupByName(StringView name)
    {
        return Groups.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public void ResetSelection(string from, string? to)
    {
        if (MountRouletteGroup == from)
        {
            MountRouletteGroup = to;
        }

        if (FlyingMountRouletteGroup == from)
        {
            FlyingMountRouletteGroup = to;
        }
    }

    public void AddGroup(string name)
    {
        Groups.Add(new MountGroup { Name = name });
    }
}
