namespace BetterMountRoulette.Util;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

using System;
using System.Linq;
using System.Reflection;

using Addon = FFXIVClientStructs.Attributes.Addon;

internal sealed class CastBarHelper : IDisposable
{
    private (int IconID, SeString Text)? _regularMountRoulette;
    private (int IconID, SeString Text)? _flyingMountRoulette;

    private (long IconID, string Text)? _lastCastInfo;
    private uint? _lastMountActionID;
    private bool _shouldUpdate;

    private bool? _show;
    private readonly Services _services;

    public CastBarHelper(Services services)
    {
        _services = services;
    }

    public bool? Show
    {
        get => _show;
        set
        {
            _show = value;
            _shouldUpdate = true;
        }
    }

    public uint MountID { get; set; }
    public bool IsFlyingRoulette { get; set; }

    public unsafe void Enable()
    {
        _services.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_CastBar", CastBarOnUpdate);
    }

    private static bool IsNullOr(uint? value, uint comparand)
    {
        return value is null || value == comparand;
    }

    private unsafe void CastBarOnUpdate(AddonEvent _, AddonArgs args)
    {
        if (!_shouldUpdate)
        {
            return;
        }

        _shouldUpdate = false;

        if (Show is null || (Show is true && _lastMountActionID != MountID))
        {
            _show = null;
            _lastMountActionID = null;
            _lastCastInfo = null;
            return;
        }

        _lastMountActionID = MountID;

        if (Show is false && _regularMountRoulette is null)
        {
            ExcelSheet<GeneralAction>? sheet = _services.GameData.GetExcelSheet<GeneralAction>();
            GeneralAction? mountRouletteAction = sheet!.GetRow(9);
            _regularMountRoulette = (mountRouletteAction!.Icon, mountRouletteAction.Name);

            mountRouletteAction = sheet.GetRow(24);
            _flyingMountRoulette = (mountRouletteAction!.Icon, mountRouletteAction.Name);
        }

        // un-hiding mount doesn't work cleanly.
        // implicitly unhiding works best usually, but not at all if the same mount
        // is selected first by roulette and then again manually
        UpdateCastBarInternal((AddonCastBar*)args.Addon);
    }

    private unsafe void UpdateCastBarInternal(AddonCastBar* castBar)
    {
        if (castBar->AtkUnitBase.UldManager.NodeList == null || castBar->AtkUnitBase.UldManager.NodeListCount < 8)
        {
            // cast bar is configured weirdly. ignore.
            return;
        }

        var icon = (AtkComponentNode*)castBar->AtkUnitBase.GetNodeById(8u);
        AtkTextNode* skillNameText = castBar->AtkUnitBase.GetTextNodeById(4u);
        var component = (AtkComponentIcon*)icon->Component;

        if (Show is false)
        {
            // store current cast info for restoring later if necessary
            _lastCastInfo = (component->IconId, skillNameText->NodeText.ToString());

            // replace cast bar contents with mount roulette information.
            (int IconID, SeString Text)? mountRoulette = IsFlyingRoulette ? _flyingMountRoulette : _regularMountRoulette;
            int iconID = mountRoulette!.Value.IconID;
            ReadOnlySpan<byte> text = mountRoulette.Value.Text.RawData;

            component->IconId = iconID;
            component->IconImage->LoadIconTexture((uint)iconID, 0);
            skillNameText->SetText(text);
        }
        else if (_lastCastInfo is { } /* same as "not null", but nameable */ castInfo)
        {
            // restore previous cast info
            // TODO: find the place in the game code where the transformation from
            // TODO: (actiontype, actionid) to (icon, text) happens and just call
            // TODO: that instead.
            component->IconId = castInfo.IconID;
            component->IconImage->LoadIconTexture((uint)component->IconId, 0);
            skillNameText->SetText(castInfo.Text);
            _lastCastInfo = null;
        }
    }

    private unsafe void ResetCastBar()
    {
        if (_lastCastInfo is null)
        {
            // last cast wasn't replaced mount roulette, nothing to do.
            return;
        }

        AddonCastBar* castBar = GetUnitBase<AddonCastBar>();
        if (castBar is null)
        {
            return;
        }

        _show = null;
        UpdateCastBarInternal(castBar);
    }

    private unsafe T* GetUnitBase<T>(string? name = null, int index = 1) where T : unmanaged
    {
        if (string.IsNullOrEmpty(name))
        {
            if (typeof(T).GetCustomAttribute(typeof(Addon)) is Addon attr)
            {
                name = attr.AddonIdentifiers.FirstOrDefault();
            }
        }

        return string.IsNullOrEmpty(name)
            ? default
            : (T*)_services.GameGui.GetAddonByName(name, index);
    }

    public void Dispose()
    {
        _services.AddonLifecycle.UnregisterListener(CastBarOnUpdate);
        ResetCastBar();
    }
}
