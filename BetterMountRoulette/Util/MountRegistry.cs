namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;

using BetterRouletteBase.Util;

using FFXIVClientStructs.FFXIV.Client.Game.Group;

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Responsible for maintaining a list of mounts with ID, name, icon, and whether or not the mount is unlocked.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection")]
internal sealed class MountRegistry(PluginServices services) : ItemRegistry<MountData, MountGroup>(services.ClientState)
{
    private readonly PluginServices _services = services;

    private string[]? _fastMountNames;
    private int _largestExtraSeatCount;

    public int UnlockedMountCount { get; private set; }

    protected override IEnumerable<MountData> GetAllItems()
    {
        return from mount in _services.GameData.GetExcelSheet<Mount>()
               where mount.UIPriority > 0 && mount.Icon != 0 /* valid mounts only */
               orderby mount.UIPriority, mount.RowId
               select new MountData(_services.TextureProvider, mount.Singular)
               {
                   IconID = mount.Icon,
                   ID = mount.RowId,
                   Unlocked = _services.GameFunctions.HasMountUnlocked(mount.RowId),
                   ExtraSeats = mount.ExtraSeats,
                   IsFast = mount.RowId is 71 or 318 /* TODO: find a better way to get this info */
               };
    }

    protected override bool IsItemUnlocked(uint id)
    {
        return _services.GameFunctions.HasMountUnlocked(id);
    }

    protected override void GatherExtraItemData(MountData item)
    {
        _largestExtraSeatCount = Math.Max(item.ExtraSeats, _largestExtraSeatCount);
    }

    protected override List<MountData> FilterAvailableItems(List<MountData> items, MountGroup group)
    {
        switch (group.FastMode)
        {
            case FastMode.On:
            case FastMode.IfGrounded when !_services.GameFunctions.IsFlightUnlocked():
                (byte maxSpeed, byte currentSpeed) = _services.GameFunctions.GetCurrentTerritoryMountSpeedInfo();

                if (maxSpeed > 0 && currentSpeed == 0)
                {
                    items.NonClearingUnsortedFindAllInPlace(x => x.IsFast);
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
                ? Math.Min(_largestExtraSeatCount, partySize - 1)
                : 1;

            items.NonClearingUnsortedFindAllInPlace(x => x.ExtraSeats >= extraSeats);
        }
        else if (multiseatSettings.SingleSeatWhileSolo && partySize <= 1)
        {
            items.NonClearingUnsortedFindAllInPlace(x => x.ExtraSeats == 0);
        }

        return items;
    }

    internal IEnumerable<string> GetFastMountNames()
    {
        InitializeIfNecessary();
        return _fastMountNames ??= InternalItems.Where(x => x.IsFast).Select(x => x.CapitalizedName).ToArray();
    }
}
