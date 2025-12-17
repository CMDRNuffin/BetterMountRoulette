namespace BetterMountRoulette.Util.Hooks;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using System;
using System.Runtime.InteropServices;

internal sealed class AgentMountNoteBookHooks : IDisposable
{
    private bool _disposedValue;
    private readonly PluginServices _services;
    private readonly Hook<Delegates.UseRoulette> _agentMountNoteBookUseRouletteHook;
    private readonly Hook<Delegates.GetRouletteIcon> _agentMountNoteBookGetRouletteIconHook;
    private readonly Hook<Delegates.GetRouletteActionId> _agentMountNoteBookGetRouletteActionIdHook;
    private readonly Hook<Delegates.CanUseRoulette> _agentMountNoteBookIsRouletteAvailableHook;

    public unsafe AgentMountNoteBookHooks(PluginServices services)
    {
        _services = services;

        AgentInterface* agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.MountNotebook);
        var vtable = (AgentMountNoteBookVTable*)agent->VirtualTable;
        _agentMountNoteBookUseRouletteHook = services.GameInteropProvider.HookFromAddress<Delegates.UseRoulette>(
            vtable->UseRoulette,
            UseRoulette);
        _agentMountNoteBookGetRouletteIconHook = services.GameInteropProvider.HookFromAddress<Delegates.GetRouletteIcon>(
            vtable->GetRouletteIcon,
            GetRouletteIcon);
        _agentMountNoteBookGetRouletteActionIdHook = services.GameInteropProvider.HookFromAddress<Delegates.GetRouletteActionId>(
            vtable->GetRouletteActionId,
            GetRouletteActionId);
        _agentMountNoteBookIsRouletteAvailableHook = services.GameInteropProvider.HookFromAddress<Delegates.CanUseRoulette>(
            vtable->CanUseRoulette,
            CanUseRoulette);
    }

    internal void Enable()
    {
        _agentMountNoteBookUseRouletteHook.Enable();
        _agentMountNoteBookGetRouletteIconHook.Enable();
        _agentMountNoteBookGetRouletteActionIdHook.Enable();
        _agentMountNoteBookIsRouletteAvailableHook.Enable();
    }

    internal void Disable()
    {
        _agentMountNoteBookUseRouletteHook.Disable();
        _agentMountNoteBookGetRouletteIconHook.Disable();
        _agentMountNoteBookGetRouletteActionIdHook.Disable();
        _agentMountNoteBookIsRouletteAvailableHook.Disable();
    }

    private unsafe bool CanUseRoulette(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnIsRouletteAvailable(this, {rouletteIndex})");
        if (rouletteIndex == 1)
        {
            rouletteIndex = 0;
        }

        return _agentMountNoteBookIsRouletteAvailableHook.Original(@this, rouletteIndex);
    }

    private unsafe uint GetRouletteActionId(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnGetRouletteActionId(this, {rouletteIndex})");
        return rouletteIndex == 1
            ? 24
            : _agentMountNoteBookGetRouletteActionIdHook.Original(@this, rouletteIndex);
    }

    private unsafe uint GetRouletteIcon(AgentInterface* @this, uint rouletteIndex)
    {
        _services.PluginLog.Debug($"OnGetRouletteIcon(this, {rouletteIndex})");
        return rouletteIndex == 1
            ? 122
            : _agentMountNoteBookGetRouletteIconHook.Original(@this, rouletteIndex);
    }

    private unsafe bool UseRoulette(AgentInterface* @this, uint rouletteIndex)
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

    private static class Delegates
    {
        public unsafe delegate bool UseRoulette(AgentInterface* @this, uint rouletteIndex);
        public unsafe delegate uint GetRouletteIcon(AgentInterface* @this, uint rouletteIndex);
        public unsafe delegate uint GetRouletteActionId(AgentInterface* @this, uint rouletteIndex);
        public unsafe delegate bool CanUseRoulette(AgentInterface* @this, uint rouletteIndex);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x160)]
    private unsafe struct AgentMountNoteBookVTable
    {
        [FieldOffset(0x90)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, bool> UseRoulette;

        [FieldOffset(0xC0)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, bool> CanUseRoulette;

        [FieldOffset(0xC8)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, uint> GetRouletteActionId;

        [FieldOffset(0xD0)]
        public unsafe delegate* unmanaged<AgentInterface*, uint, uint> GetRouletteIcon;
    }
}
