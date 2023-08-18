namespace BetterMountRoulette.Config.Data;

using Newtonsoft.Json;

using System.Collections.Generic;

internal sealed class MountGroup
{
    [JsonProperty("Name")]
    public string Name { get; set; } = "";

    [JsonProperty("EnabledMounts")]
    public HashSet<uint> IncludedMounts { get; set; } = new();

    [JsonProperty("IncludeNewMounts")]
    public bool IncludedMeansActive { get; set; }

    [JsonProperty("ForceMultiseatersInParty")]
    public bool ForceMultiseatersInParty { get; set; } = false;

    [JsonProperty("PreferMoreSeats")]
    public bool PreferMoreSeats { get; set; } = false;

    [JsonProperty("ForceSingleSeatersWhileSolo")]
    public bool ForceSingleSeatersWhileSolo { get; set; } = false;
}
