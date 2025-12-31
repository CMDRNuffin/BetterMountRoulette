namespace BetterMountRoulette.Util;

using BetterMountRoulette.Config.Data;

using BetterRouletteBase.UI;
using BetterRouletteBase.Util;

using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game;

internal sealed class ActionHandler : ActionHandlerBase
{
    private const uint NORMAL_ROULETTE_ACTION_ID = 9;
    private const uint FLYING_ROULETTE_ACTION_ID = 24;

    private static readonly ConditionFlag[] _ignoreActionConditions = [
        ConditionFlag.Mounted,
        ConditionFlag.RidingPillion,
    ];
    private readonly PluginServices _services;
    private readonly MountRegistry _mountRegistry;
    private readonly GameFunctions _gameFunctions;
    private RouletteDisplayType? _displayTypeOverride;

    public ActionHandler(PluginServices services, MountRegistry mountRegistry) : base(services.GameInteropProvider, services.PluginLog)
    {
        _services = services;
        _mountRegistry = mountRegistry;
        _gameFunctions = _services.GameFunctions;
    }

    protected unsafe override bool OnUseAction(UseActionArgs args)
    {
        RouletteDisplayType? displayTypeOverride = _displayTypeOverride;
        _displayTypeOverride = null;

        if (_services.Condition.Any(_ignoreActionConditions)
            || CharacterConfig is not { } characterConfig)
        {
            return args.Original();
        }

        string? groupName = (args.ActionID, args.ActionType) switch
        {
            (NORMAL_ROULETTE_ACTION_ID, ActionType.GeneralAction) => characterConfig.MountRouletteGroup,
            (FLYING_ROULETTE_ACTION_ID, ActionType.GeneralAction) => characterConfig.FlyingMountRouletteGroup,
            _ => null,
        };

        ActionType oldActionType = args.ActionType;
        uint oldActionId = args.ActionID;
        if (groupName is not null)
        {
            MountGroup? mountGroup = CharacterConfig.GetGroupByName(groupName);

            uint newActionID = 0;
            if (mountGroup is not null)
            {
                newActionID = _mountRegistry.GetRandom(ActionManager.Instance(), mountGroup);
            }

            if (newActionID is not 0)
            {
                args.ActionType = ActionType.Mount;
                args.ActionID = newActionID;
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
                case FLYING_ROULETTE_ACTION_ID when args.ActionType != oldActionType:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.FlyingRoulette;
                    break;
                case NORMAL_ROULETTE_ACTION_ID when args.ActionType != oldActionType:
                    _gameFunctions.NextMountRouletteOverride = MountRouletteOverride.NormalRoulette;
                    break;
                default:
                    // no-op
                    break;
            }
        }

        return args.Original();
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

        arguments = RenameItemDialog.NormalizeWhiteSpace(arguments).ToString();

        MountGroup? mountGroup = characterConfig.GetGroupByName(arguments);
        if (mountGroup == null)
        {
            // handle quotes because not doing that in the first place was a dumb decision
            if (arguments.StartsWith('"') && arguments.EndsWith('"'))
            {
                arguments = arguments[1..^1];
            }

            mountGroup = characterConfig.GetGroupByName(arguments);
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

    protected override void DisposeInternal(bool disposing)
    {
    }
}
