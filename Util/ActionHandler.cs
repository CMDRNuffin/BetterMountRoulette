namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.UI;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;

using System;

internal sealed class ActionHandler : IDisposable
{
    private readonly Services _services;
    private readonly MountRegistry _mountRegistry;
    private readonly Hook<UseActionHandler>? _useActionHook;
    private readonly CastBarHelper _castBarHelper;
    private (bool hide, uint actionID) _hideAction;
    private bool _disposedValue;

    public unsafe ActionHandler(
        Services services,
        WindowManager windowManager,
        MountRegistry mountRegistry)
    {
        _services = services;
        _mountRegistry = mountRegistry;
        _castBarHelper = new CastBarHelper(_services);
        nint renderAddress = (nint)ActionManager.Addresses.UseAction.Value;
        if (renderAddress is 0)
        {
            windowManager.DebugWindow.Broken("Unable to load UseAction address");
            return;
        }

        _useActionHook = Hook<UseActionHandler>.FromAddress(renderAddress, OnUseAction);
        _useActionHook.Enable();
        _castBarHelper.Enable();
    }

    public unsafe delegate byte UseActionHandler(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID = 3758096384U, uint a4 = 0U, uint a5 = 0U, uint a6 = 0U, void* a7 = default);

    private unsafe byte OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7)
    {
        (bool hide, uint hideActionID) = _hideAction;
        _hideAction = (false, 0);

        if (_services.Condition[ConditionFlag.Mounted]
            || _services.Condition[ConditionFlag.Mounted2]
            || CharacterConfig is not { } characterConfig
            || (characterConfig.MountRouletteGroup is null && characterConfig.FlyingMountRouletteGroup is null))
        {
            return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
        }

        string? groupName = (actionID, actionType) switch
        {
            (9, ActionType.General) => CharacterConfig.MountRouletteGroup,
            (24, ActionType.General) => CharacterConfig.FlyingMountRouletteGroup,
            _ => null,
        };

        bool isRouletteActionID = actionID is 9 or 24;
        ActionType oldActionType = actionType;
        uint oldActionId = actionID;
        if (groupName is not null)
        {
            MountGroup? mountGroup = CharacterConfig.GetMountGroup(groupName);

            uint newActionID = 0;
            if (mountGroup is not null)
            {
                newActionID = _mountRegistry.GetRandom(ActionManager.Instance(), mountGroup);
            }

            if (newActionID is not 0)
            {
                actionType = ActionType.Mount;
                actionID = newActionID;
            }
        }

        if (hide)
        {
            oldActionId = hideActionID;
            oldActionType = ActionType.General;
            isRouletteActionID = true;
        }

        switch (oldActionType)
        {
            case ActionType.General when isRouletteActionID && actionType != oldActionType:
                _castBarHelper.Show = false;
                _castBarHelper.IsFlyingRoulette = oldActionId == 24;
                _castBarHelper.MountID = actionID;
                break;
            case ActionType.Mount:
                _castBarHelper.Show = true;
                _castBarHelper.MountID = actionID;
                break;
        }

        byte result = _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);

        return result;
    }

    public CharacterConfig? CharacterConfig { get; set; }

    public unsafe void HandleMountCommand(string command, string arguments)
    {
        if (CharacterConfig is not { } characterConfig)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(arguments))
        {
            _services.Chat.PrintError("Please specify a mount group");
            return;
        }

        arguments = RenameItemDialog.NormalizeWhiteSpace(arguments);

        MountGroup? mountGroup = characterConfig.GetMountGroup(arguments);
        if (mountGroup == null)
        {
            _services.Chat.PrintError($"Mount group \"{arguments}\" not found.");
            return;
        }

        uint mount = _mountRegistry.GetRandom(ActionManager.Instance(), mountGroup);
        if (mount is not 0)
        {
            _hideAction = (true, actionID: 9);
            _ = ActionManager.Instance()->UseAction(ActionType.Mount, mount);
            _services.Chat.Print($"Mounting {mount}");
        }
        else
        {
            _services.Chat.PrintError($"Unable to summon mount from group \"{arguments}\".");
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _useActionHook?.Dispose();
            _castBarHelper.Dispose();

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ActionHandler()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
