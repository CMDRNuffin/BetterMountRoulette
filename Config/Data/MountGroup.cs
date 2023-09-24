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
}
