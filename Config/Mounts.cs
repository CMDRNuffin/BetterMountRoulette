namespace BetterMountRoulette.Config;

using BetterMountRoulette.UI;
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

    private static volatile bool _isInitialized;
    private static readonly object _initLock = new();

    private static readonly Dictionary<uint, MountData> _mountsByID = new();
    private static readonly List<MountData> _mounts = new();
    private readonly List<MountSelectionData> _selectableMounts;
    private readonly Dictionary<uint, MountSelectionData> _selectableMountsByID;
    private static readonly Random _random = new();

    private List<MountSelectionData>? _filteredMounts;
    private static readonly Dictionary<string, Mounts> _instancesByGroup = new(StringComparer.InvariantCultureIgnoreCase);
    private static Configuration _config = new();

    private static void InitializeIfNecessary()
    {
        // make sure initialization only runs once
        if (_isInitialized)
        {
            return;
        }

        lock (_initLock)
        {
            // make sure initialization only runs once
            // (again, in case multiple threads called this at the same time)
            if (_isInitialized)
            {
                return;
            }

            _mounts.AddRange(GetAllMounts());
            foreach (var mount in _mounts)
            {
                _mountsByID.Add(mount.ID, mount);
            }

            _isInitialized = true;
        }
    }

    public static WindowManager WindowManager { get; set; } = null!;

    private static IEnumerable<MountData> GetAllMounts()
    {
        return from mount in BetterMountRoulettePlugin.GameData.GetExcelSheet<Mount>()
               where mount.UIPriority > 0 && mount.Icon != 0 /* valid mounts only */
               orderby mount.UIPriority, mount.RowId
               select new MountData
               {
                   IconID = mount.Icon,
                   ID = mount.RowId,
                   Name = mount.Singular,
               };
    }

    private Mounts()
    {
        InitializeIfNecessary();

        _selectableMounts = _mounts.ConvertAll(x => new MountSelectionData(x, true));
        _selectableMountsByID = _selectableMounts.ToDictionary(x => x.Mount.ID);

        RefreshUnlocked();
    }

    public int ItemCount => (_filteredMounts ?? _selectableMounts).Count;

    public static Mounts? GetInstance(string name)
    {
        if (_instancesByGroup.TryGetValue(name, out var value))
        {
            return value;
        }
        else
        {
            var mountGroup = _config.GetMountGroup(name);

            if (mountGroup is null)
            {
                return null;
            }

            value = new();
            _instancesByGroup[name] = value;
            value.Load(mountGroup);
            return value;
        }
    }

    public int PageCount
    {
        get
        {
            var cnt = ItemCount;
            return cnt / PAGE_SIZE + (cnt % PAGE_SIZE == 0 ? 0 : 1);
        }
    }

    internal static void Load(Configuration config)
    {
        _config = config;
        foreach (var group in config.Groups.Concat(new[] { new DefaultMountGroup(config) }))
        {
            var item = new Mounts();
            item.Load(group);
            _instancesByGroup[group.Name] = item;
        }
    }

    internal static void Remove(string groupName)
    {
        _ = _instancesByGroup.Remove(groupName);
    }

    private void Load(MountGroup mountGroup)
    {
        _selectableMounts.ForEach(x => x.Enabled = false);
        mountGroup.EnabledMounts.ForEach(x =>
        {
            if (_selectableMountsByID.TryGetValue(x, out var mount))
            {
                mount.Enabled = true;
            }
        });

        UpdateUnlocked(mountGroup.IncludeNewMounts);
    }

    internal void Save(MountGroup config)
    {
        config.EnabledMounts = _selectableMounts.Where(x => x.Enabled).Select(x => x.Mount.ID).ToList();
    }

    public static unsafe void RefreshUnlocked()
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

    private List<MountSelectionData> GetPage(int page)
    {
        return (_filteredMounts ?? _selectableMounts)
            .Skip((page - 1) * PAGE_SIZE)
            .Take(PAGE_SIZE)
            .ToList();
    }

    private IEnumerable<MountSelectionData> FilteredMounts(bool showUnlocked, bool? enabledStatus, string? filter)
    {
        IEnumerable<MountSelectionData> mounts = _selectableMounts;
        if (!showUnlocked)
        {
            mounts = mounts.Where(x => x.Mount.Unlocked);
        }

        if (enabledStatus is not null)
        {
            mounts = mounts.Where(x => x.Enabled == enabledStatus);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            mounts = mounts.Where(x => x.Mount.Name.RawString.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
        }

        return mounts;
    }

    public static (uint IconID, SeString Name) GetCastBarInfo(uint mountID)
    {
        var mount = _mountsByID[mountID];
        return (mount.IconID, mount.Name);
    }

    internal uint GetRandom(Pointer<ActionManager> actionManager)
    {
        RefreshUnlocked();

        var availableMounts = _selectableMounts.Where(x => x.IsAvailable(actionManager)).ToList();
        if (!availableMounts.Any())
        {
            return 0;
        }

#pragma warning disable CA5394 // Do not use insecure randomness
        // no secure randomness required
        var index = _random.Next(availableMounts.Count);
#pragma warning restore CA5394 // Do not use insecure randomness

        return availableMounts[index].Mount.ID;
    }

    internal void UpdateUnlocked(bool enableNewMounts)
    {
        foreach (var mount in _selectableMounts)
        {
            if (!mount.Mount.Unlocked)
            {
                mount.Enabled = enableNewMounts;
            }
        }
    }

    internal void Update(bool enabled, int? page = null)
    {
        var list = page is null
            ? _selectableMounts.Where(x => x.Mount.Unlocked).ToList()
            : GetPage(page.Value);
        list.ForEach(x => x.Enabled = enabled);
    }

    private class MountSelectionData
    {
        public MountSelectionData(MountData mount, bool enabled)
        {
            Mount = mount;
            Enabled = enabled;
        }

        public MountData Mount { get; }
        public bool Enabled { get; set; }

        public void Render()
        {
            Enabled = Mount.Render(Enabled);
        }

        public bool IsAvailable(Pointer<ActionManager> actionManager)
        {
            return Enabled && Mount.IsAvailable(actionManager);
        }
    }

    private class MountData
    {
        private IntPtr? _mountIcon;
        private static IntPtr? _selectedUnselectedIcon;

        public uint ID { get; set; }
        public uint IconID { get; set; }
        public SeString Name { get; init; } = null!;
        public bool Unlocked { get; set; }

        public bool Render(bool enabled)
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
                enabled ^= true;
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

            Vector2 offset = new(enabled ? 0.1f : 0.6f, 0.2f);
            Vector2 offset2 = new(enabled ? 0.4f : 0.9f, 0.8f);
            ImGui.Image(_selectedUnselectedIcon!.Value, overlaySize, offset, offset2);

            // put cursor back to where it was after rendering the button to prevent
            // messing up the table rendering
            ImGui.SetCursorPos(finalPos);

            return enabled;
        }

        private void LoadImages()
        {
            _mountIcon ??= TextureHelper.LoadIconTexture(IconID);
            _selectedUnselectedIcon ??= TextureHelper.LoadUldTexture("readycheck");
        }

        public unsafe bool IsAvailable(Pointer<ActionManager> actionManager)
        {
            return Unlocked && actionManager.Value->GetActionStatus(ActionType.Mount, ID) == 0;
        }
    }
}
