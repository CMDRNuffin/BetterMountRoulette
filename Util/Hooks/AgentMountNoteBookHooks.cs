namespace BetterMountRoulette.Util.Hooks;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using System;
using System.Runtime.InteropServices;

internal sealed class AgentMountNoteBookHooks : IDisposable
{
    private bool _disposedValue;
    private readonly Services _services;
    private readonly Hook<AgentMountNoteBookUseRouletteDetour> _agentMountNoteBookUseRouletteHook;
    private readonly Hook<AgentMountNoteBookGetRouletteIconDetour> _agentMountNoteBookGetRouletteIconHook;
    private readonly Hook<AgentMountNoteBookGetRouletteActionIdDetour> _agentMountNoteBookGetRouletteActionIdHook;
    private readonly Hook<AgentMountNoteBookIsRouletteAvailableDetour> _agentMountNoteBookIsRouletteAvailableHook;

    private unsafe delegate bool AgentMountNoteBookUseRouletteDetour(AgentInterface* @this, uint rouletteIndex);
    private unsafe delegate uint AgentMountNoteBookGetRouletteIconDetour(AgentInterface* @this, uint rouletteIndex);
    private unsafe delegate uint AgentMountNoteBookGetRouletteActionIdDetour(AgentInterface* @this, uint rouletteIndex);
    private unsafe delegate bool AgentMountNoteBookIsRouletteAvailableDetour(AgentInterface* @this, uint rouletteIndex);

    public unsafe AgentMountNoteBookHooks(Services services)
    {
        _services = services;

        AgentInterface* agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.MountNotebook);
        var vtable = (AgentMountNoteBookVTable*)agent->VirtualTable;
        _agentMountNoteBookUseRouletteHook = services.GameInteropProvider.HookFromAddress<AgentMountNoteBookUseRouletteDetour>(
            vtable->UseRoulette,
            OnUseRoulette);
        _agentMountNoteBookGetRouletteIconHook = services.GameInteropProvider.HookFromAddress<AgentMountNoteBookGetRouletteIconDetour>(
            vtable->GetRouletteIcon,
            OnGetRouletteIcon);
        _agentMountNoteBookGetRouletteActionIdHook = services.GameInteropProvider.HookFromAddress<AgentMountNoteBookGetRouletteActionIdDetour>(
            vtable->GetRouletteActionId,
            OnGetRouletteActionId);
        _agentMountNoteBookIsRouletteAvailableHook = services.GameInteropProvider.HookFromAddress<AgentMountNoteBookIsRouletteAvailableDetour>(
            vtable->IsRouletteAvailable,
            OnIsRouletteAvailable);

        _agentMountNoteBookUseRouletteHook.Enable();
        _agentMountNoteBookGetRouletteIconHook.Enable();
        _agentMountNoteBookGetRouletteActionIdHook.Enable();
        _agentMountNoteBookIsRouletteAvailableHook.Enable();
    }

    private unsafe bool OnIsRouletteAvailable(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnIsRouletteAvailable(this, {rouletteIndex})");
        if (rouletteIndex == 1)
        {
            rouletteIndex = 0;
        }

        return _agentMountNoteBookIsRouletteAvailableHook.Original(@this, rouletteIndex);
    }

    private unsafe uint OnGetRouletteActionId(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnGetRouletteActionId(this, {rouletteIndex})");
        return rouletteIndex == 1
            ? 24
            : _agentMountNoteBookGetRouletteActionIdHook.Original(@this, rouletteIndex);
    }

    private unsafe uint OnGetRouletteIcon(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnGetRouletteIcon(this, {rouletteIndex})");
        return rouletteIndex == 1
            ? 122
            : _agentMountNoteBookGetRouletteIconHook.Original(@this, rouletteIndex);
    }

    private unsafe bool OnUseRoulette(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnUseRoulette(this, {rouletteIndex})");
        return rouletteIndex == 1
            ? ActionManager.Instance()->UseAction(ActionType.GeneralAction, 24)
            : _agentMountNoteBookUseRouletteHook.Original(@this, rouletteIndex);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _agentMountNoteBookUseRouletteHook.Dispose();
            _agentMountNoteBookGetRouletteIconHook.Dispose();
            _agentMountNoteBookGetRouletteActionIdHook.Dispose();
            _agentMountNoteBookIsRouletteAvailableHook.Dispose();

            _disposedValue = true;
        }
    }

    ~AgentMountNoteBookHooks()
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

    [StructLayout(LayoutKind.Explicit, Size = 0x158)]
    private unsafe struct AgentMountNoteBookVTable
    {
        [FieldOffset(0x88)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, bool> UseRoulette;

        [FieldOffset(0xB8)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, bool> IsRouletteAvailable;

        [FieldOffset(0xC0)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, uint> GetRouletteActionId;

        [FieldOffset(0xC8)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, uint> GetRouletteIcon;
    }
}
