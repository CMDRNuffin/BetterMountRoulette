namespace BetterMountRoulette.Config.Data;

using Dalamud.Configuration;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal sealed class Configuration : IPluginConfiguration
{
    public const int CONFIG_VERSION = 3;
    public const ulong DUMMY_LEGACY_CONFIG_ID = 0;
    public const string DEFAULT_GROUP_NAME = "Default";

    [Versions(introduced: 0)]
    public int Version { get; set; }

    [Versions(introduced: 0, removed: 2)]
    public bool Enabled { get; set; }

    [Versions(introduced: 0, removed: 3)]
    public HashSet<uint> EnabledMounts { get; set; } = [];

    [Versions(introduced: 3)]
    public Dictionary<ulong, CharacterConfigEntry> CharacterConfigs { get; set; } = [];

    [Versions(introduced: 3)]
    public int? NewCharacterHandling { get; set; }

    [Versions(introduced: 0)]
    public string DefaultGroupName { get; set; } = DEFAULT_GROUP_NAME;

    [JsonIgnore]
    public bool HasNonDefaultGroups => Groups.Count > 0;

    [Versions(introduced: 0, removed: 2)]
    public bool IncludeNewMounts { get; set; } = true;

    [Versions(introduced: 2, removed: 3)]
    public List<MountGroup> Groups { get; set; } = [];

    [Versions(introduced: 2, removed: 3)]
    public string? MountRouletteGroup { get; set; }

    [Versions(introduced: 2, removed: 3)]
    public string? FlyingMountRouletteGroup { get; set; }

    public static Configuration Init()
    {
        return new Configuration { Version = CONFIG_VERSION, NewCharacterHandling = NewCharacterHandlingModes.BLANK };
    }

    public static class NewCharacterHandlingModes
    {
        public const int ASK = 0;
        public const int IMPORT = 1;
        public const int BLANK = 2;
    }
}

public class CharacterConfigEntry
{
    [field: MaybeNull]
    public string CharacterName
    {
        get => field ?? "INVALID CONFIG";
        set;
    }

    [field: MaybeNull]
    public string CharacterWorld
    {
        get => field ?? "INVALID CONFIG";
        set;
    }

    public string FileName { get; set; } = "";
}
