namespace BetterMountRoulette.Config.Data;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

internal sealed class CharacterConfig
{
    [JsonIgnore]
    public bool IsNew { get; set; }
    public bool IncludeNewMounts { get; set; } = true;

    public List<MountGroup> Groups { get; set; } = new();

    public string? MountRouletteGroup { get; set; }

    public string? FlyingMountRouletteGroup { get; set; }

    [JsonIgnore]
    public bool HasNonDefaultGroups => Groups.Count > 1;

    public void CopyFrom(CharacterConfig other)
    {
        IncludeNewMounts = other.IncludeNewMounts;
        Groups = other.Groups;
        MountRouletteGroup = other.MountRouletteGroup;
        FlyingMountRouletteGroup = other.FlyingMountRouletteGroup;
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal string comparison",
        Justification = "We actually want string normalization here, to ensure same behavior as in the duplicate check when renaming or adding a group")]
    public MountGroup? GetMountGroup(string name)
    {
        return Groups.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }
}
