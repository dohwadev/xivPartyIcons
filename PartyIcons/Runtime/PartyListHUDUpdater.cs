﻿using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    public bool UpdateHUD = false;

    private readonly Settings _configuration;
    private readonly PartyListHUDView _view;
    private readonly RoleTracker _roleTracker;

    private bool _displayingRoles;

    private bool _previousInParty;
    private bool _previousTesting;
    private DateTime _lastUpdate = DateTime.Today;

    private const string PrepareZoningSig = "48 89 5C 24 ?? 57 48 83 EC 40 F6 42 0D 08";
    private delegate nint PrepareZoningDelegate (nint a1, nint a2, byte a3);
    private Hook<PrepareZoningDelegate> prepareZoningHook;

    public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Settings configuration)
    {
        _view = view;
        _roleTracker = roleTracker;
        _configuration = configuration;
        
        prepareZoningHook =
            Hook<PrepareZoningDelegate>.FromAddress(Service.SigScanner.ScanText(PrepareZoningSig), PrepareZoning);
        prepareZoningHook?.Enable();
    }

    public void Enable()
    {
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
        Service.Framework.Update += OnUpdate;
        _configuration.OnSave += OnConfigurationSave;
        Service.ClientState.EnterPvP += OnEnterPvP;
    }

    public void Dispose()
    {
        Service.ClientState.EnterPvP -= OnEnterPvP;
        _configuration.OnSave -= OnConfigurationSave;
        Service.Framework.Update -= OnUpdate;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
        prepareZoningHook?.Dispose();
    }

    private nint PrepareZoning(nint a1, nint a2, byte a3)
    {
        PluginLog.Verbose("PartyListHUDUpdater Forcing update due to zoning");
        // PluginLog.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
        return prepareZoningHook.OriginalDisposeSafe(a1,a2,a3);
    }

    private void OnEnterPvP()
    {
        if (_displayingRoles)
        {
            PluginLog.Verbose("PartyListHUDUpdater: reverting party list due to entering a PvP zone");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }
    }

    private void OnConfigurationSave()
    {
        if (_displayingRoles)
        {
            PluginLog.Verbose("PartyListHUDUpdater: reverting party list before the update due to config change");
            _view.RevertSlotNumbers();
        }

        PluginLog.Verbose("PartyListHUDUpdater forcing update due to changes in the config");
        // PluginLog.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }

    private void OnAssignedRolesUpdated()
    {
        PluginLog.Verbose("PartyListHUDUpdater forcing update due to assignments update");
        // PluginLog.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }
    
    private void OnUpdate(Framework framework)
    {
        var inParty = Service.PartyList.Any();

        if ((!inParty && _previousInParty) || (!_configuration.TestingMode && _previousTesting))
        {
            PluginLog.Verbose("No longer in party/testing mode, reverting party list HUD changes");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }

        _previousInParty = inParty;
        _previousTesting = _configuration.TestingMode;

        if (DateTime.Now - _lastUpdate > TimeSpan.FromSeconds(15))
        {
            UpdatePartyListHUD();
            _lastUpdate = DateTime.Now;
        }
    }

    private void UpdatePartyListHUD()
    {
        if (!_configuration.DisplayRoleInPartyList)
        {
            return;
        }

        if (_configuration.TestingMode &&
            Service.ClientState.LocalPlayer is { } localPlayer)
        {
            _view.SetPartyMemberRole(localPlayer.Name.ToString(), localPlayer.ObjectId, RoleId.M1);
        }

        if (!UpdateHUD)
        {
            return;
        }

        if (Service.ClientState.IsPvP)
        {
            return;
        }

        PluginLog.Verbose($"Updating party list HUD. members = {Service.PartyList.Length}");
        _displayingRoles = true;

        foreach (var member in Service.PartyList)
        {
            PluginLog.Verbose($"member {member.Name.ToString()}");
            
            if (_roleTracker.TryGetAssignedRole(member.Name.ToString(), member.World.Id, out var roleId))
            {
                PluginLog.Verbose($"Updating party list hud: member {member.Name} to {roleId}");
                _view.SetPartyMemberRole(member.Name.ToString(), member.ObjectId, roleId);
            }
            else
            {
                PluginLog.Verbose($"Could not get assigned role for member {member.Name.ToString()}, {member.World.Id}");
            }
        }
    }
}
