using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

using global::HLStatsX.Contract;

namespace HLStatsX;

[PluginMetadata(
    Id = "HLStatsX",
    Version = "1.2.7",
    Name = "HLStatsX:CE Ingame Plugin (SwiftlyS2)",
    Author = "SyntX34",
    Description = "Provides CS2 in-game interaction and messaging with HLstatsX:CE daemon"
)]

public partial class HLStatsX : BasePlugin, IHLStatsXApi
{
    private static readonly Dictionary<ulong, Dictionary<string, WeaponStats>> _Statsme = new();
    private static readonly Dictionary<ulong, Dictionary<string, HitgroupStats>> _Statsme2 = new();
    private readonly Dictionary<int, CCSCustomHudLayout> _playerHuds = new();
    private readonly HashSet<int> _activeHudPlayers = new();
    private readonly Dictionary<int, CancellationTokenSource> _menuCloseTimers = new();
    private readonly Dictionary<int, IMenu> _playerMenus = new();
    private IMenu? _builtinMainMenu;
    private readonly Dictionary<int, CCSCustomHudLayout> _csayHuds = new();
    private readonly Dictionary<int, CancellationTokenSource> _csayTimers = new();
    private readonly Dictionary<int, CCSCustomHudLayout> _tsayHuds = new();
    private readonly Dictionary<int, CancellationTokenSource> _tsayTimers = new();
    private readonly Dictionary<int, (string Title, List<string> Lines, int Page)> _customHudStatsState = new();
    private readonly Dictionary<ulong, int> _playerStreak = new();
    private readonly HashSet<int> _mapConnectedPlayers = new();
    private readonly HashSet<int> _mapAnnouncedPlayers = new();
    private readonly Dictionary<ulong, string> _playerClanTags = new();

    private string _protectAddress = "";
    private bool _blockChatCommands = true;
    private string _messagePrefix = "";
    private string _webpageUrl = "";
    private string _hlxceVersion = "";
    private string _currentMapName = "";
    private Guid _chatHookGuid = Guid.Empty;
    private Guid _clientCmdHookGuid = Guid.Empty;
    private readonly Dictionary<int, (string Command, long Tick)> _lastPlayerCommand = new();

    private readonly HashSet<string> _interactingPlugins = new(StringComparer.OrdinalIgnoreCase);

    public event Action? OnHLStatsXLoaded;
    public string PluginVersion => "1.2.7";

    public HLStatsX(ISwiftlyCore core) : base(core)
    {
    }

    public void RegisterConsumer(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName)) return;
        if (_interactingPlugins.Add(pluginName.Trim()))
        {
            Console.WriteLine($"[HLstatsX:CE] Plugin '{pluginName.Trim()}' registered with HLStatsX API (Total interacting: {_interactingPlugins.Count})");
        }
    }

    public void TriggerPlayerAction(IPlayer player, string actionCode)
    {
        if (player != null && player.IsValid && !string.IsNullOrWhiteSpace(actionCode))
            SendLog(player, actionCode, "triggered");
    }

    public void TriggerTeamAction(string teamName, string actionCode)
    {
        if (!string.IsNullOrWhiteSpace(teamName) && !string.IsNullOrWhiteSpace(actionCode))
            SendLog(null, $"Team \"{teamName}\" triggered \"{actionCode}\"", null);
    }

    public void TriggerWorldAction(string actionCode)
    {
        if (!string.IsNullOrWhiteSpace(actionCode))
            SendLog(null, $"World triggered \"{actionCode}\"", null);
    }

    void IHLStatsXApi.SendUdpLog(string logLine)
    {
        SendUdpLog(logLine);
    }

    void IHLStatsXApi.OpenStatsMenu(IPlayer player)
    {
        OpenStatsMenu(player);
    }

    void IHLStatsXApi.ShowCenterHud(IPlayer? target, string message, float duration)
    {
        ShowCenterHud(target, message, duration);
    }

    void IHLStatsXApi.ShowTopLeftHud(IPlayer? target, string message, float duration)
    {
        ShowTopLeftHud(target, message, duration);
    }

    void IHLStatsXApi.AddPlayerKillCredit(IPlayer attacker, IPlayer? victim)
    {
        if (attacker == null || !attacker.IsValid) return;

        if (victim != null && victim.IsValid)
            FlushPlayerKillStreak(victim);

        if (attacker.SteamID == 0 || attacker.IsFakeClient) return;
        _playerStreak.TryGetValue(attacker.SteamID, out int curStreak);
        _playerStreak[attacker.SteamID] = curStreak + 1;
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        try
        {
            interfaceManager.AddSharedInterface<IHLStatsXApi, HLStatsX>("HlStatsX.API", this);
            Console.WriteLine("[HLstatsX:CE] Shared interface 'HlStatsX.API' registered.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Failed to register shared interface: {ex.Message}");
        }
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
    }

    public override void Load(bool hotReload)
    {
        LoadConfig();

        EnsureUdpConnection();
        RegisterServerCommands();
        RegisterGameEvents();
        SendServerCvars();
        AnnounceCurrentMap();
        AnnounceAllConnectedPlayers();
        Console.WriteLine($"[HLstatsX:CE] Loaded. Sending logs to {_config.DaemonHost}:{_config.DaemonPort} (Server: {_config.ServerIp}:{_config.ServerPort}, ReceiverPort: {_config.ReceiverPort}, MaxPlayers: {_config.MaxPlayers}, Debug: {_config.Debug})");
        Console.WriteLine($"[HLstatsX:CE] API 'HlStatsX.API' ready. Active plugins interacting: {_interactingPlugins.Count}");
        OnHLStatsXLoaded?.Invoke();

        Core.Scheduler.DelayBySeconds(3.0f, () =>
        {
            Console.WriteLine($"[HLstatsX:CE] Post-load API status: {_interactingPlugins.Count} plugin(s) interacting: [{string.Join(", ", _interactingPlugins)}]");
        });
    }

    public override void Unload()
    {
        UnregisterGameEvents();
        ClearAllMapState();
        StopUdpConnection();
        Console.WriteLine("[HLstatsX:CE] Unloaded.");
    }
}
