namespace BetterMountRoulette.Config;

using BetterMountRoulette.Util;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.STD;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal class Mounts
{
    private const int PAGE_SIZE = COLUMNS * ROWS;
    private const int COLUMNS = 5;
    private const int ROWS = 6;

    private readonly List<MountData> _mounts;
    private readonly Dictionary<uint, MountData> _mountsByID;
    private static readonly Random _random = new();

    private List<MountData>? _filteredMounts;
    private static Mounts? _instance;

    public Mounts()
    {
        _mounts = (from mount in BetterMountRoulettePlugin.GameData.GetExcelSheet<Mount>()
                   where mount.UIPriority > 0 && mount.Icon != 0 /* valid mounts only */
                   orderby mount.UIPriority, mount.RowId
                   select new MountData
                   {
                       IconID = mount.Icon,
                       ID = mount.RowId,
                       Name = mount.Singular,
                       Enabled = true,
                   }).ToList();

        _mountsByID = _mounts.ToDictionary(x => x.ID);

        RefreshUnlocked();
    }

    public static Mounts Instance => _instance ??= new();

    public int ItemCount => (_filteredMounts ?? _mounts).Count;

    public int PageCount
    {
        get
        {
            var cnt = ItemCount;
            return cnt / PAGE_SIZE + (cnt % PAGE_SIZE == 0 ? 0 : 1);
        }
    }

    internal void Load(Configuration config)
    {
        _mounts.ForEach(x => x.Enabled = false);
        config.EnabledMounts.ForEach(x =>
        {
            if (_mountsByID.TryGetValue(x, out var mount))
            {
                mount.Enabled = true;
            }
        });
    }

    internal void Save(Configuration config)
    {
        config.EnabledMounts = _mounts.Where(x => x.Enabled).Select(x => x.ID).ToList();
    }

    public unsafe void RefreshUnlocked()
    {
        foreach (var mount in _mounts)
        {
            mount.Unlocked = GameFunctions.HasMountUnlocked(mount.ID);
        }
    }

    public void Filter(bool showLocked, bool? enabledStatus, string? filterText)
    {
        _filteredMounts = AsList(FilteredMounts(showLocked, enabledStatus, filterText));
    }

    public void ClearFilter()
    {
        _filteredMounts = null;
    }

    private static List<T> AsList<T>(IEnumerable<T> source)
    {
        return source as List<T> ?? source.ToList();
    }

    public void RenderItems(int page)
    {
        _ = ImGui.BeginTable("tbl_mounts", COLUMNS);

        GetPage(page).ForEach(x => x.Render());

        ImGui.EndTable();
    }

    private List<MountData> GetPage(int page)
    {
        return (_filteredMounts ?? _mounts)
            .Skip((page - 1) * PAGE_SIZE)
            .Take(PAGE_SIZE)
            .ToList();
    }

    private IEnumerable<MountData> FilteredMounts(bool showUnlocked, bool? enabledStatus, string? filter)
    {
        IEnumerable<MountData> mounts = _mounts;
        if (!showUnlocked)
        {
            mounts = mounts.Where(x => x.Unlocked);
        }

        if (enabledStatus is not null)
        {
            mounts = mounts.Where(x => x.Enabled == enabledStatus);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            mounts = mounts.Where(x => x.Name.RawString.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
        }

        return mounts;
    }

    public (uint IconID, SeString Name) GetCastBarInfo(uint mountID)
    {
        var mount = _mountsByID[mountID];
        return (mount.IconID, mount.Name);
    }

    internal uint GetRandom(Pointer<ActionManager> actionManager)
    {
        RefreshUnlocked();

        var availableMounts = _mounts.Where(x => x.IsAvailable(actionManager)).ToList();
        if (!availableMounts.Any())
        {
            return 0;
        }

#pragma warning disable CA5394 // Do not use insecure randomness
        // no secure randomness required
        var index = _random.Next(availableMounts.Count);
#pragma warning restore CA5394 // Do not use insecure randomness

        return availableMounts[index].ID;
    }

    internal void UpdateUnlocked(bool enableNewMounts)
    {
        foreach (var mount in _mounts)
        {
            if (!mount.Unlocked)
            {
                mount.Enabled = enableNewMounts;
            }
        }
    }

    internal void Update(bool enabled, int? page = null)
    {
        var list = page is null
            ? _mounts.Where(x => x.Unlocked).ToList()
            : GetPage(page.Value);
        list.ForEach(x => x.Enabled = enabled);
    }

    private class MountData
    {
        private IntPtr? _mountIcon;
        private static IntPtr? _selectedUnselectedIcon;

        public uint ID { get; set; }
        public uint IconID { get; set; }
        public SeString Name { get; init; } = null!;
        public bool Enabled { get; set; }
        public bool Unlocked { get; set; }

        public void Render()
        {
            _ = ImGui.TableNextColumn();

            LoadImages();

            var originalPos = ImGui.GetCursorPos();

            const float ButtonSize = 60f;
            const float OverlaySize = 24f;
            const float OverlayOffset = 4f;
            var buttonSize = new Vector2(ButtonSize);
            var overlaySize = new Vector2(OverlaySize);

            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

            if (ImGui.ImageButton(_mountIcon!.Value, buttonSize, Vector2.Zero, Vector2.One, 0))
            {
                Enabled ^= true;
            }

            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Name.RawString);
            }

            var finalPos = ImGui.GetCursorPos();

            // calculate overlay (top right corner) position
            var overlayPos = originalPos + new Vector2(buttonSize.X - overlaySize.X + OverlayOffset, 0);
            ImGui.SetCursorPos(overlayPos);

            Vector2 offset = new(Enabled ? 0.1f : 0.6f, 0.2f);
            Vector2 offset2 = new(Enabled ? 0.4f : 0.9f, 0.8f);
            ImGui.Image(_selectedUnselectedIcon!.Value, overlaySize, offset, offset2);

            // put cursor back to where it was after rendering the button to prevent
            // messing up the table rendering
            ImGui.SetCursorPos(finalPos);
        }

        private void LoadImages()
        {
            _mountIcon ??= TextureHelper.LoadIconTexture(IconID);
            _selectedUnselectedIcon ??= TextureHelper.LoadUldTexture("readycheck");
        }

        public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
        {
            return Enabled && Unlocked && actionManager.Value->GetActionStatus(ActionType.Mount, ID) == 0;
        }
    }
}
