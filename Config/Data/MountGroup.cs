namespace BetterMountRoulette.Config.Data;

using Newtonsoft.Json;

using System.Collections.Generic;

internal sealed class MountGroup
{
    public string Name { get; set; } = "";

    [JsonProperty(PropertyName = "EnabledMounts")]
    public HashSet<uint> IncludedMounts { get; set; } = new();

    [JsonProperty(PropertyName = "IncludeNewMounts")]
    public bool IncludedMeansActive { get; set; }
}
