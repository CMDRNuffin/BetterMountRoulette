namespace BetterMountRoulette.Config;

using BetterMountRoulette.Config.Data;

using System.Linq;

internal static class MountGroupManager
{
    public static void Delete(CharacterConfig config, string name)
    {
        for (int i = 0; i < config.Groups.Count; ++i)
        {
            if (name == config.Groups[i].Name)
            {
                config.Groups.RemoveAt(i);
                break;
            }
        }

        if (config.MountRouletteGroup == name)
        {
            config.MountRouletteGroup = config.Groups.FirstOrDefault()?.Name;
        }

        if (config.FlyingMountRouletteGroup == name)
        {
            config.FlyingMountRouletteGroup = config.Groups.FirstOrDefault()?.Name;
        }
    }

    public static void Rename(CharacterConfig config, string currentName, string newName)
    {
        if (config.MountRouletteGroup == currentName)
        {
            config.MountRouletteGroup = newName;
        }

        if (config.FlyingMountRouletteGroup == currentName)
        {
            config.FlyingMountRouletteGroup = newName;
        }

        MountGroup? group = config.Groups.FirstOrDefault(x => x.Name == currentName);
        if (group is { } g)
        {
            g.Name = newName;
        }
    }
}
