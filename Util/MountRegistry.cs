namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Responsible for maintaining a list of mounts with ID, name, icon, and whether or not the mount is unlocked.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class MountRegistry
{
    private readonly Services _services;
    private readonly Dictionary<uint, MountData> _mountsByID = new();
    private readonly List<MountData> _mounts = new();
    private bool _isInitialized;
    private readonly object _lock = new();

    public int UnlockedMountCount { get; private set; }

    public MountRegistry(Services services)
    {
        _services = services;
    }

    private void InitializeIfNecessary()
    {
        // make sure initialization only runs once
        if (_isInitialized)
        {
            return;
        }

        lock (_lock)
        {
            // make sure initialization only runs once
            // (again, in case multiple threads called this at the same time)
            if (_isInitialized)
            {
                return;
            }

            _mounts.AddRange(GetAllMounts());
            foreach (MountData mount in _mounts)
            {
                _mountsByID.Add(mount.ID, mount);
            }

            _isInitialized = true;
        }
    }

    public void RefreshUnlocked()
    {
        if (!_services.ClientState.IsLoggedIn)
        {
            return;
        }

        InitializeIfNecessary();
        int count = 0;
        foreach (MountData mount in _mounts)
        {
            if (mount.Unlocked = GameFunctions.HasMountUnlocked(mount.ID))
            {
                ++count;
            }
        }

        UnlockedMountCount = count;
    }

    public IEnumerable<MountData> GetAllMounts()
    {
        return from mount in _services.GameData.GetExcelSheet<Mount>()
               where mount.UIPriority > 0 && mount.Icon != 0 /* valid mounts only */
               orderby mount.UIPriority, mount.RowId
               select new MountData(_services.TextureHelper, mount.Singular)
               {
                   IconID = mount.Icon,
                   ID = mount.RowId,
                   Unlocked = GameFunctions.HasMountUnlocked(mount.RowId),
                   ExtraSeats = mount.ExtraSeats,
               };
    }

    public List<MountData> GetUnlockedMounts()
    {
        InitializeIfNecessary();
        return _mounts.Where(x => x.Unlocked).ToList();
    }

    public List<MountData> GetAvailableMounts(Pointer<ActionManager> actionManager, MountGroup group)
    {
        RefreshUnlocked();
        List<MountData> unlockedMounts = GetUnlockedMounts();
        if (group.IncludedMeansActive)
        {
            return unlockedMounts.FindAll(x => group.IncludedMounts.Contains(x.ID) && x.IsAvailable(actionManager));
        }

        return unlockedMounts.FindAll(x => !group.IncludedMounts.Contains(x.ID) && x.IsAvailable(actionManager));
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-critical use of randomness, so we prefer speed over security")]
    public uint GetRandom(Pointer<ActionManager> actionManager, MountGroup group)
    {
        List<MountData> available = GetAvailableMounts(actionManager, group);
        if (available.Count is 0)
        {
            return 0;
        }

        int index = Random.Shared.Next(available.Count);
        return available[index].ID;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-critical use of randomness, so we prefer speed over security")]
    public uint GetRandomWithExtraSeats(Pointer<ActionManager> actionManager, MountGroup group, int extraSeats = 1)
    {
        List<MountData> available = GetAvailableMounts(actionManager, group);
        var withExtraSeats = available.Where(x => x.ExtraSeats >= extraSeats).ToList();

        if (withExtraSeats.Count > 0)
        {
            int index = Random.Shared.Next(withExtraSeats.Count);
            return withExtraSeats[index].ID;
        }
        else if (available.Count is 0)
        {
            return 0;
        }
        else
        {
            // Fall back to regular mounts if none have extra seats.
            int index = Random.Shared.Next(available.Count);
            return available[index].ID;
        }
    }
}
