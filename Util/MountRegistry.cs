namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.Interop;

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Responsible for maintaining a list of mounts with ID, name, icon, and whether or not the mount is unlocked.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class MountRegistry(PluginServices services)
{
    private readonly PluginServices _services = services;
    private readonly Dictionary<uint, MountData> _mountsByID = [];
    private readonly List<MountData> _mounts = [];
    private readonly List<MountData> _unlockedMounts = [];
    private readonly List<MountData> _availableMounts = [];

    private bool _isInitialized;
    private readonly object _lock = new();
    private string[]? _fastMountNames;

    public int UnlockedMountCount { get; private set; }

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
        _unlockedMounts.Clear();
        foreach (MountData mount in _mounts)
        {
            if (mount.Unlocked = _services.GameFunctions.HasMountUnlocked(mount.ID))
            {
                _unlockedMounts.Add(mount);
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
                   Unlocked = _services.GameFunctions.HasMountUnlocked(mount.RowId),
                   ExtraSeats = mount.ExtraSeats,
                   IsFast = mount.RowId is 71 or 318 /* TODO: find a better way to get this info */
               };
    }

    public List<MountData> GetUnlockedMounts()
    {
        InitializeIfNecessary();
        return _unlockedMounts;
    }

    public List<MountData> GetAvailableMounts(Pointer<ActionManager> actionManager, MountGroup group, out int largestExtraSeatCount)
    {
        RefreshUnlocked();
        List<MountData> unlockedMounts = GetUnlockedMounts();
        _availableMounts.Clear();
        List<MountData> result = _availableMounts;
        largestExtraSeatCount = 0;

        foreach (MountData item in unlockedMounts)
        {
            if (group.IncludedMounts.Contains(item.ID) == group.IncludedMeansActive && item.IsAvailable(actionManager))
            {
                result.Add(item);
                largestExtraSeatCount = Math.Max(item.ExtraSeats, largestExtraSeatCount);
            }
        }

        return result;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-critical use of randomness, so we prefer speed over security")]
    public uint GetRandom(Pointer<ActionManager> actionManager, MountGroup group)
    {
        List<MountData> available = GetAvailableMounts(actionManager, group, out int largestExtraSeatNumber);

        switch (group.FastMode)
        {
            case FastMode.On:
            case FastMode.IfGrounded when !_services.GameFunctions.IsFlightUnlocked():
                (byte maxSpeed, byte currentSpeed) = _services.GameFunctions.GetCurrentTerritoryMountSpeedInfo();

                if (maxSpeed > 0 && currentSpeed == 0)
                {
                    available.NonClearingUnsortedFindAllInPlace(x => x.IsFast);
                }

                break;
            case FastMode.IfGrounded:
            // but flight is unlocked, so no-op
            case FastMode.Off:
            default:
                // no-op
                break;
        }

        int partySize;
        unsafe
        {
            partySize = GroupManager.Instance()->MainGroup.MemberCount;
        }

        MultiseatSettings multiseatSettings = group.GetMultiSeatSettings(_services.ClientState.IsPvP);

        if (multiseatSettings.MultiSeatInParty && partySize > 1)
        {
            // If the largest unlocked mount has more extra seats than other people in the party,
            // only use mounts that can accomodate the entire party. Otherwise use only mounts with
            // the largest available number of seats.
            int extraSeats = multiseatSettings.PreferMoreSeats
                ? Math.Min(largestExtraSeatNumber, partySize - 1)
                : 1;

            available.NonClearingUnsortedFindAllInPlace(x => x.ExtraSeats >= extraSeats);
        }
        else if (multiseatSettings.SingleSeatWhileSolo && partySize <= 1)
        {
            available.NonClearingUnsortedFindAllInPlace(x => x.ExtraSeats == 0);
        }

        if (available.Count is 0)
        {
            // shortcut: no active mounts, can't select anything
            return 0;
        }

        if (available.Count is 1)
        {
            // shortcut: exactly one active mount: can only select that, no matter what
            return available[0].ID;
        }

        int index = Random.Shared.Next(available.Count);
        return available[index].ID;
    }

    internal IEnumerable<string> GetFastMountNames()
    {
        InitializeIfNecessary();
        return _fastMountNames ??= _mounts.Where(x => x.IsFast).Select(x => x.CapitalizedName).ToArray();
    }
}
