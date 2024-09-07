namespace BetterMountRoulette.Config.Data;

using Newtonsoft.Json;

using System.Collections.Generic;

internal sealed class MountGroup
{
    public string Name { get; set; } = "";

    [JsonProperty("EnabledMounts")]
    public HashSet<uint> IncludedMounts { get; set; } = new();

    [JsonProperty("IncludeNewMounts")]
    public bool IncludedMeansActive { get; set; }

    public bool ForceMultiseatersInParty { get; set; }

    public bool PreferMoreSeats { get; set; }

    public bool ForceSingleSeatersWhileSolo { get; set; }

    public RouletteDisplayType DisplayType { get; set; }

    public FastMode FastMode { get; set; }
}

public enum FastMode
{
    Off = 0,
    IfGrounded = 1,
    On = 2,
}

public enum RouletteDisplayType
{
    Grounded,
    Flying,
    Show,
}