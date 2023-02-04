namespace BetterMountRoulette.Config;

internal sealed class CharacterConfig : ConfigurationBase
{
    public ulong CharacterID { get; set; }

    public string CharacterName { get; set; } = "";

    public string CharacterWorld { get; set; } = "";
}
