namespace BetterMountRoulette.Config.Data;

using BetterRouletteBase.Config;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;

internal sealed class MountGroup : IItemGroup
{
    public string Name { get; set; } = "";

    [JsonProperty("EnabledMounts")]
    public HashSet<uint> IncludedMounts { get; set; } = [];

    [JsonProperty("IncludeNewMounts")]
    public bool IncludedMeansActive { get; set; }

    public bool ForceMultiseatersInParty { get; set; }

    public bool PreferMoreSeats { get; set; }

    public bool ForceSingleSeatersWhileSolo { get; set; }

    public RouletteDisplayType DisplayType { get; set; }

    public FastMode FastMode { get; set; }

    public bool PvpOverrideMultiseaterSettings { get; set; }

    public bool PvpForceMultiseatersInParty { get; set; }

    public bool PvpPreferMoreSeats { get; set; }

    public bool PvpForceSingleSeatersWhileSolo { get; set; }

    [JsonIgnore]
    HashSet<uint> IItemGroup.IncludedItems => IncludedMounts;

    public MultiseatSettings GetMultiSeatSettings(bool isPvp)
    {
        if (isPvp && PvpOverrideMultiseaterSettings)
        {
            return new MultiseatSettings
            {
                MultiSeatInParty = PvpForceMultiseatersInParty,
                PreferMoreSeats = PvpPreferMoreSeats,
                SingleSeatWhileSolo = PvpForceSingleSeatersWhileSolo,
            };
        }

        return new MultiseatSettings
        {
            MultiSeatInParty = ForceMultiseatersInParty,
            PreferMoreSeats = PreferMoreSeats,
            SingleSeatWhileSolo = ForceSingleSeatersWhileSolo,
        };
    }
}

public readonly struct MultiseatSettings : IEquatable<MultiseatSettings>
{
    public bool MultiSeatInParty { get; init; }

    public bool PreferMoreSeats { get; init; }

    public bool SingleSeatWhileSolo { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is MultiseatSettings settings && Equals(settings);
    }

    public bool Equals(MultiseatSettings other)
    {
        return MultiSeatInParty == other.MultiSeatInParty
            && PreferMoreSeats == other.PreferMoreSeats
            && SingleSeatWhileSolo == other.SingleSeatWhileSolo;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MultiSeatInParty, PreferMoreSeats, SingleSeatWhileSolo);
    }

    public static bool operator ==(MultiseatSettings left, MultiseatSettings right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MultiseatSettings left, MultiseatSettings right)
    {
        return !(left == right);
    }
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