namespace BetterMountRoulette.Config;

using System.Collections.Generic;

internal class MountGroup
{
    public virtual string Name { get; set; } = "";

    public virtual List<uint> EnabledMounts { get; set; } = new();

    public virtual bool IncludeNewMounts { get; set; } = true;
}

internal sealed class DefaultMountGroup : MountGroup
{
    private readonly ConfigurationBase _config;

    public DefaultMountGroup(ConfigurationBase config)
    {
        _config = config;
    }

    public override string Name => _config.DefaultGroupName;

    public override List<uint> EnabledMounts
    {
        get => _config.EnabledMounts;
        set => _config.EnabledMounts = value;
    }

    public override bool IncludeNewMounts
    {
        get => _config.IncludeNewMounts;
        set => _config.IncludeNewMounts = value;
    }
}
