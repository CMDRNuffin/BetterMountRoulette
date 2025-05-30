﻿namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;
using BetterMountRoulette.UI;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;

using System;

internal sealed class ActionHandler : IDisposable
{
    private const uint NORMAL_ROULETTE_ACTION_ID = 9;
    private const uint FLYING_ROULETTE_ACTION_ID = 24;

    private readonly PluginServices _services;
    private readonly MountRegistry _mountRegistry;
    private readonly Hook<UseActionHandler>? _useActionHook;
    private readonly GameFunctions _gameFunctions;
    private RouletteDisplayType? _displayTypeOverride;
    private bool _disposedValue;

    public unsafe ActionHandler(
        PluginServices services,
        MountRegistry mountRegistry)
    {
        _services = services;
        _mountRegistry = mountRegistry;
        _gameFunctions = _services.GameFunctions;
        void* renderAddress = ActionManager.MemberFunctionPointers.UseAction;
        if (renderAddress is null)
        {
            _services.PluginLog.Debug("Unable to load UseAction address");
            return;
        }

        _useActionHook = _services.GameInteropProvider.HookFromAddress<UseActionHandler>(renderAddress, OnUseAction);
        _useActionHook.Enable();
    }

    public unsafe delegate byte UseActionHandler(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID = 3758096384U, uint a4 = 0U, uint a5 = 0U, uint a6 = 0U, void* a7 = default);

    private unsafe byte OnUseAction(ActionManager* actionManager, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7)
    {
        RouletteDisplayType? displayTypeOverride = _displayTypeOverride;
        _displayTypeOverride = null;

        if (_services.Condition[ConditionFlag.Mounted]
            || _services.Condition[ConditionFlag.Mounted2]
            || CharacterConfig is not { } characterConfig)
        {
            return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
        }

        (string? groupName, bool isRouletteActionID) = (actionID, actionType) switch
        {
            (NORMAL_ROULETTE_ACTION_ID, ActionType.GeneralAction) => (CharacterConfig.MountRouletteGroup, true),
            (FLYING_ROULETTE_ACTION_ID, ActionType.GeneralAction) => (CharacterConfig.FlyingMountRouletteGroup, true),
            _ => (null, false),
        };

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

        if (displayTypeOverride is { } displayType)
        {
            switch (displayType)
            {
                case RouletteDisplayType.Grounded:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.NormalRoulette;
                    break;
                case RouletteDisplayType.Flying:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.FlyingRoulette;
                    break;
                case RouletteDisplayType.Show:
                default:
                    // no-op
                    break;
            }
        }
        else if (oldActionType == ActionType.GeneralAction)
        {
            switch (oldActionId)
            {
                case FLYING_ROULETTE_ACTION_ID when CharacterConfig.RevealMountsFlying:
                case NORMAL_ROULETTE_ACTION_ID when CharacterConfig.RevealMountsNormal:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.PlainMount;
                    break;
                case FLYING_ROULETTE_ACTION_ID when actionType != oldActionType:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.FlyingRoulette;
                    break;
                case NORMAL_ROULETTE_ACTION_ID when actionType != oldActionType:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.NormalRoulette;
                    break;
                default:
                    // no-op
                    break;
            }
        }

        return _useActionHook!.Original(actionManager, actionType, actionID, targetID, a4, a5, a6, a7);
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
            PrintError("Please specify a mount group");
            return;
        }

        arguments = RenameItemDialog.NormalizeWhiteSpace(arguments);

        MountGroup? mountGroup = characterConfig.GetMountGroup(arguments);
        if (mountGroup == null)
        {
            // handle quotes because not doing that in the first place was a dumb decision
            if (arguments.StartsWith('"') && arguments.EndsWith('"'))
            {
                arguments = arguments[1..^1];
            }

            mountGroup = characterConfig.GetMountGroup(arguments);
            if (mountGroup == null)
            {
                PrintError($"Mount group \"{arguments}\" not found.");
                return;
            }
        }

        uint mount = _mountRegistry.GetRandom(ActionManager.Instance(), mountGroup);
        if (mount is not 0)
        {
            _displayTypeOverride = mountGroup.DisplayType;
            _ = ActionManager.Instance()->UseAction(ActionType.Mount, mount);
        }
        else
        {
            PrintError($"Unable to summon mount from group \"{arguments}\".");
        }
    }

    internal void PrintError(string message)
    {
        _services.PluginLog.Error(message);
        if (!(CharacterConfig?.SuppressChatErrors ?? false))
        {
            _services.Chat.PrintError(message);
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
