namespace BetterMountRoulette.Config;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using BetterRouletteBase.Config;

using System.Collections.Generic;
using System.Linq;

internal sealed class CharacterManager(PluginServices services, Configuration configuration)
    : CharacterManagerBase<Configuration, CharacterConfig>(services.PluginLog, services.DalamudPluginInterface, services.PlayerState, configuration)
{
    private readonly PluginServices _services = services;
    private readonly Configuration _configuration = configuration;

    protected override void ImportFromConfig(CharacterConfig current, CharacterConfig toImport)
    {
        current.CopyFrom(toImport);
    }

    protected override CharacterConfig? LoadCharacterConfig(ulong playerID, CharacterConfigEntry cce)
    {
        return playerID == Configuration.DUMMY_LEGACY_CONFIG_ID
            ? LoadLegacyCharacterConfig()
            : LoadCharacterConfig(cce);
    }

    private CharacterConfig LoadLegacyCharacterConfig()
    {
        CharacterConfig result = new()
        {
            FlyingMountRouletteGroup = _configuration.FlyingMountRouletteGroup,
            MountRouletteGroup = _configuration.MountRouletteGroup,
        };

        var reg = new MountRegistry(_services);
        reg.RefreshUnlocked();
        var allMounts = reg.GetUnlockedItems().Select(x => x.ID).ToHashSet();

        AddGroup(
            result.Groups,
            allMounts,
            _configuration.DefaultGroupName,
            !_configuration.IncludeNewMounts,
            _configuration.EnabledMounts);

        foreach (MountGroup group in _configuration.Groups)
        {
            // "IncludeNewMounts" meant we would just save all non-unlocked mounts as enabled
            // while now we would just save all disabled mounts instead
            AddGroup(result.Groups, allMounts, group.Name, !group.IncludedMeansActive, group.IncludedMounts);
        }

        return result;

        static void AddGroup(
            List<MountGroup> groups,
            HashSet<uint> allMounts,
            string name,
            bool includedMeansActive,
            HashSet<uint> includedMounts)
        {
            MountGroup newGroup = new()
            {
                IncludedMeansActive = includedMeansActive,
                Name = name,
            };

            groups.Add(newGroup);
            if (includedMeansActive /* Previously "IncludeNewMounts" */)
            {
                newGroup.IncludedMounts.UnionWith(includedMounts);
            }
            else
            {
                // "IncludeNewMounts" meant we would just save all non-unlocked mounts as enabled
                // so now we just save all disabled mounts instead
                newGroup.IncludedMounts.UnionWith(allMounts);
                newGroup.IncludedMounts.ExceptWith(includedMounts);
            }
        }
    }

    protected override CharacterConfig CreateCharacterConfig()
    {
        return new() /* todo: defaults */
        {
            Groups =
            [
                new()
                {
                    Name = Configuration.DEFAULT_GROUP_NAME,
                }
            ],
            IsNew = true,
        };
    }
}
