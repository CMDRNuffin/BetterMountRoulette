namespace BetterMountRoulette.Config;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.Util;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;
using System.Linq;

internal sealed class CharacterManager(PluginServices services, Configuration configuration)
{
    private readonly PluginServices _services = services;
    private readonly Configuration _configuration = configuration;
    private CharacterConfig? _characterConfig;
    private ulong? _playerID;

    private IPluginLog PluginLog => _services.PluginLog;

    public CharacterConfig GetCharacterConfig(ulong playerID, IPlayerCharacter character)
    {
        if (_characterConfig is { } cfg && playerID == _playerID)
        {
            return cfg;
        }

        _playerID = playerID;
        _characterConfig = null;
        if (_configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce))
        {
            _characterConfig = LoadCharacterConfig(cce);
        }

        if (_characterConfig is null)
        {
            _characterConfig = CreateCharacterConfig();
            cce = new CharacterConfigEntry
            {
                CharacterName = character.Name.TextValue,
                CharacterWorld = character.HomeWorld.Value.Name.ExtractText() ?? "",
            };

            cce.FileName = $"{playerID}_{cce.CharacterName.Replace(' ', '_')}@{cce.CharacterWorld}.json";
            _configuration.CharacterConfigs[playerID] = cce;

            SaveCurrentCharacterConfig(cce);
            _services.DalamudPluginInterface.SavePluginConfig(_configuration);
        }

        return _characterConfig;
    }

    public bool Import(ulong fromPlayerID)
    {
        PluginLog.Debug($"Importing {fromPlayerID}");
        if (fromPlayerID == _playerID || _playerID is not ulong currentPlayer)
        {
            PluginLog.Debug($"No use importing from current character");
            // importing from yourself is a noop and should therefore always succeed
            return true;
        }

        CharacterConfig? characterConfig = LoadCharacterConfig(fromPlayerID);
        if (characterConfig is null || _characterConfig is null)
        {
            List<string> items = [];
            if (characterConfig is null)
            {
                items.Add("imported config is null");
            }

            if (_characterConfig is null)
            {
                items.Add("current config is null");
            }

            PluginLog.Debug($"Unable to import: {string.Join(", ", items)}");

            return false;
        }

        CharacterConfigEntry cce = _configuration.CharacterConfigs[currentPlayer];

        _characterConfig.CopyFrom(characterConfig);
        SaveCurrentCharacterConfig(cce);

        PluginLog.Debug($"Import successful");
        return true;
    }

    public void SaveCurrentCharacterConfig()
    {
        if (_playerID is not ulong playerID)
        {
            return;
        }

        CharacterConfigEntry cce = _configuration.CharacterConfigs[playerID];
        SaveCurrentCharacterConfig(cce);
    }

    private void SaveCharacterConfig(CharacterConfigEntry entry, CharacterConfig config)
    {
        string dir = GetCharConfigDir();
        if (!Directory.Exists(dir))
        {
            _ = Directory.CreateDirectory(dir);
        }

        File.WriteAllText(Path.Combine(dir, entry.FileName), JsonConvert.SerializeObject(config));
    }

    private CharacterConfig? LoadCharacterConfig(ulong playerID)
    {
        if (_configuration.CharacterConfigs.TryGetValue(playerID, out CharacterConfigEntry? cce))
        {
            CharacterConfig? res = playerID == Configuration.DUMMY_LEGACY_CONFIG_ID
                ? LoadLegacyCharacterConfig()
                : LoadCharacterConfig(cce);

            return res;
        }

        return null;
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
        var allMounts = reg.GetUnlockedMounts().Select(x => x.ID).ToHashSet();

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
            if (newGroup.IncludedMeansActive /* Previously "IncludeNewMounts" */)
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

    private void SaveCurrentCharacterConfig(CharacterConfigEntry entry)
    {
        if (_characterConfig is { } charConfig)
        {
            SaveCharacterConfig(entry, charConfig);
        }
    }

    private CharacterConfig? LoadCharacterConfig(CharacterConfigEntry cce)
    {
        if (cce.FileName is not null /* can still be null if freshly loaded */)
        {
            string path = Path.Combine(GetCharConfigDir(), cce.FileName);

            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<CharacterConfig>(File.ReadAllText(path));
                }
                catch (IOException /* file deleted in the meantime. shouldn't happen, but technically can */)
                {
                }
            }
        }

        return null;
    }

    private static CharacterConfig CreateCharacterConfig()
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

    private string GetCharConfigDir()
    {
        return _services.DalamudPluginInterface.GetPluginConfigDirectory();
    }
}
