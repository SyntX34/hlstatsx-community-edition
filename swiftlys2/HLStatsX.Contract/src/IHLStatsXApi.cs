using System;
using SwiftlyS2.Shared.Players;

namespace HLStatsX.Contract;

/// <summary>
/// Public shared contract interface for HLStatsX:CE CS2 API.
/// Other plugins consume this via IInterfaceManager.GetSharedInterface<IHLStatsXApi>("HlStatsX.API").
/// </summary>
public interface IHLStatsXApi
{
    /// <summary>
    /// Registers a consuming plugin for tracking and diagnostics.
    /// </summary>
    void RegisterConsumer(string pluginName);

    /// <summary>
    /// Triggered when HLStatsX:CE daemon is connected and receiver is ready.
    /// </summary>
    event Action? OnHLStatsXLoaded;

    /// <summary>
    /// The plugin version string.
    /// </summary>
    string PluginVersion { get; }

    /// <summary>
    /// Log an action/event for a player to HLStatsX:CE (awards or deducts points configured in web panel).
    /// Format emitted: "Name<userid><steamid><team>" triggered "actionCode"
    /// </summary>
    void TriggerPlayerAction(IPlayer player, string actionCode);

    /// <summary>
    /// Log a team action/event to HLStatsX:CE.
    /// Format emitted: Team "teamName" triggered "actionCode"
    /// </summary>
    void TriggerTeamAction(string teamName, string actionCode);

    /// <summary>
    /// Log a world/server action/event to HLStatsX:CE.
    /// Format emitted: World triggered "actionCode"
    /// </summary>
    void TriggerWorldAction(string actionCode);

    /// <summary>
    /// Send a custom log line directly to the HLStatsX daemon.
    /// </summary>
    void SendUdpLog(string logLine);

    /// <summary>
    /// Displays an interactive stats menu or HUD popup to a player.
    /// </summary>
    void OpenStatsMenu(IPlayer player);

    /// <summary>
    /// Shows a center HUD message on screen.
    /// </summary>
    void ShowCenterHud(IPlayer? target, string message, float duration = 4f);

    /// <summary>
    /// Shows a top-left HUD message on screen.
    /// </summary>
    void ShowTopLeftHud(IPlayer? target, string message, float duration = 4f);
}
