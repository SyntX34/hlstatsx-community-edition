using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HLStatsX;

[PluginMetadata(
    Id = "HLStatsX",
    Version = "1.1.2",
    Name = "HLStatsX:CE Ingame Plugin (SwiftlyS2)",
    Author = "SyntX34",
    Description = "Provides CS2 in-game interaction and messaging with HLstatsX:CE daemon"
)]

public partial class HLStatsX : BasePlugin
{
    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rank", "skill", "points", "place", "session", "session_data",
        "kpd", "kdratio", "kdeath", "next", "load", "status", "servers",
        "top20", "top10", "top5", "clans", "bans", "cheaters", "statsme",
        "weapons", "weapon", "action", "actions", "accuracy", "targets",
        "target", "kills", "kill", "player_kills", "cmd", "cmds", "command",
        "hlx_display 0", "hlx_display 1", "hlx_teams 0", "hlx_teams 1",
        "hlx_hideranking", "hlx_chat 0", "hlx_chat 1", "hlx_menu",
        "servers 1", "servers 2", "servers 3", "hlx", "hlstatsx", "help"
    };

    private static readonly HashSet<string> MenuCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "hlx", "hlstatsx", "menu", "hlxmenu", "hlx_menu", "statsmenu"
    };

    private static readonly Dictionary<string, string> WeaponCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ak47"]          = "ak47",
        ["aug"]           = "aug",
        ["awp"]           = "awp",
        ["famas"]         = "famas",
        ["g3sg1"]         = "g3sg1",
        ["galilar"]       = "galilar",
        ["m4a1_silencer"] = "m4a1_silencer",
        ["m4a1"]          = "m4a1",
        ["scar20"]        = "scar20",
        ["sg556"]         = "sg556",
        ["ssg08"]         = "ssg08",
        ["mac10"]         = "mac10",
        ["mp5sd"]         = "mp5sd",
        ["mp7"]           = "mp7",
        ["mp9"]           = "mp9",
        ["bizon"]         = "bizon",
        ["p90"]           = "p90",
        ["ump45"]         = "ump45",
        ["mag7"]          = "mag7",
        ["nova"]          = "nova",
        ["sawedoff"]      = "sawedoff",
        ["xm1014"]        = "xm1014",
        ["m249"]          = "m249",
        ["negev"]         = "negev",
        ["cz75a"]         = "cz75a",
        ["deagle"]        = "deagle",
        ["elite"]         = "elite",
        ["fiveseven"]     = "fiveseven",
        ["glock"]         = "glock",
        ["hkp2000"]       = "hkp2000",
        ["p250"]          = "p250",
        ["revolver"]      = "revolver",
        ["tec9"]          = "tec9",
        ["usp_silencer"]  = "usp_silencer",
        ["usp"]           = "usp",
        ["hegrenade"]     = "hegrenade",
        ["flashbang"]     = "flashbang",
        ["smokegrenade"]  = "smokegrenade",
        ["molotov"]       = "inferno",
        ["incgrenade"]    = "inferno",
        ["inferno"]       = "inferno",
        ["decoy"]         = "decoy",
        ["taser"]         = "taser",
        ["knife"]         = "knife",
        ["knife_t"]       = "knife_t",
        ["bayonet"]       = "bayonet",
        ["knife_butterfly"] = "knife_butterfly",
        ["knife_falchion"]  = "knife_falchion",
        ["knife_flip"]      = "knife_flip",
        ["knife_gut"]       = "knife_gut",
        ["knife_karambit"]  = "knife_karambit",
        ["knife_m9_bayonet"] = "knife_m9_bayonet",
        ["knife_tactical"]   = "knife_tactical",
        ["knife_push"]       = "knife_push",
        ["knife_survival_bowie"] = "knife_survival_bowie",
        ["knife_ursus"]      = "knife_ursus",
        ["knife_gypsy_jackknife"] = "knife_gypsy_jackknife",
        ["knife_stiletto"]   = "knife_stiletto",
        ["knife_widowmaker"] = "knife_widowmaker",
        ["knife_css"]        = "knife_css",
        ["knife_cord"]       = "knife_cord",
        ["knife_canis"]      = "knife_canis",
        ["knife_outdoor"]    = "knife_outdoor",
        ["knife_skeleton"]   = "knife_skeleton",
        ["knife_kukri"]      = "knife_kukri",
    };

    private struct WeaponStats
    {
        public int shots;
        public int hits;
        public int headshots;
        public int damage;
        public int kills;
        public int deaths;
    }

    private struct HitgroupStats
    {
        public int head;
        public int chest;
        public int stomach;
        public int leftarm;
        public int rightarm;
        public int leftleg;
        public int rightleg;
    }

    public class HLStatsXConfig
    {
        public string DaemonHost { get; set; } = "127.0.0.1";
        public int DaemonPort { get; set; } = 27500;
        public string ServerIp { get; set; } = "5.135.143.217";
        public int ServerPort { get; set; } = 27015;
        public string ProxyKey { get; set; } = "";
        public int MaxPlayers { get; set; } = 32;
        public int ReceiverPort { get; set; } = 27015;
        public string MenuType { get; set; } = "CustomHud";
        public string CustomMenuLayout { get; set; } = "resources/panorama/layout/custom_game/hlx_menu.xml";
        public int AutoCloseMenuSeconds { get; set; } = 15;
    }

    private HLStatsXConfig _config = new();
    private static readonly Dictionary<ulong, Dictionary<string, WeaponStats>> _Statsme = new();
    private static readonly Dictionary<ulong, Dictionary<string, HitgroupStats>> _Statsme2 = new();
    private readonly Dictionary<int, CCSCustomHudLayout> _playerHuds = new();
    private readonly HashSet<int> _activeHudPlayers = new();
    private readonly Dictionary<int, System.Threading.CancellationTokenSource> _menuCloseTimers = new();
    private string _protectAddress = "";
    private bool _blockChatCommands = true;
    private string _messagePrefix = "";
    private UdpClient? _udpClient;
    private UdpClient? _udpReceiver;
    private System.Threading.CancellationTokenSource? _udpCts;

    public HLStatsX(ISwiftlyCore core) : base(core)
    {
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
    }

    public override void Load(bool hotReload)
    {
        try
        {
            var cfgService = Core.Configuration
                .InitializeTomlWithModel<HLStatsXConfig>("config.toml", "HLStatsX");
            var section = cfgService.Manager.GetSection("HLStatsX");
            var host = section["DaemonHost"];
            var port = section["DaemonPort"];
            var sip = section["ServerIp"];
            var sport = section["ServerPort"];
            var pkey = section["ProxyKey"];
            if (!string.IsNullOrWhiteSpace(host)) _config.DaemonHost = host;
            if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out int p)) _config.DaemonPort = p;
            if (!string.IsNullOrWhiteSpace(sip)) _config.ServerIp = sip;
            if (!string.IsNullOrWhiteSpace(sport) && int.TryParse(sport, out int sp)) _config.ServerPort = sp;
            if (!string.IsNullOrWhiteSpace(pkey)) _config.ProxyKey = pkey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Config API load warning: {ex.Message}");
        }

        try
        {
            string cfgPath = Core.Configuration.GetConfigPath("config.toml");
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(cfgPath)) candidates.Add(cfgPath);
            try
            {
                if (!string.IsNullOrWhiteSpace(Core.Configuration.BasePath))
                {
                    candidates.Add(System.IO.Path.Combine(Core.Configuration.BasePath, "config.toml"));
                    candidates.Add(System.IO.Path.Combine(Core.Configuration.BasePath, "HLStatsX", "config.toml"));
                }
            }
            catch {}
            candidates.Add(System.IO.Path.Combine("addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(System.IO.Path.Combine("csgo", "addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(System.IO.Path.Combine(AppContext.BaseDirectory, "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(System.IO.Path.Combine(AppContext.BaseDirectory, "addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));

            string? foundPath = null;
            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c) && System.IO.File.Exists(c))
                {
                    foundPath = c;
                    break;
                }
            }

            if (foundPath != null)
            {
                Console.WriteLine($"[HLstatsX:CE] Reading config file from: {foundPath}");
                foreach (var line in System.IO.File.ReadAllLines(foundPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith('#') || trimmed.StartsWith(';') || !trimmed.Contains('=')) continue;
                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"', '\'');
                    if (key.Equals("DaemonHost", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.DaemonHost = val;
                    else if (key.Equals("DaemonPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int p)) _config.DaemonPort = p;
                    else if (key.Equals("ServerIp", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.ServerIp = val;
                    else if (key.Equals("ServerPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int sp)) _config.ServerPort = sp;
                    else if (key.Equals("ProxyKey", StringComparison.OrdinalIgnoreCase)) _config.ProxyKey = val;
                    else if (key.Equals("MaxPlayers", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int mp)) _config.MaxPlayers = mp;
                    else if (key.Equals("ReceiverPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int rp)) _config.ReceiverPort = rp;
                    else if (key.Equals("MenuType", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.MenuType = val;
                    else if (key.Equals("CustomMenuLayout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.CustomMenuLayout = val;
                    else if (key.Equals("AutoCloseMenuSeconds", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int acs)) _config.AutoCloseMenuSeconds = acs;
                }
            }
            else
            {
                Console.WriteLine("[HLstatsX:CE] Warning: config.toml could not be located on disk!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Direct file read warning: {ex.Message}");
        }

        _udpClient = new UdpClient();
        StartUdpReceiver();
        RegisterServerCommands();
        RegisterGameEvents();
        SendUdpLog($"server_cvar: \"maxplayers\" \"{_config.MaxPlayers}\"");
        foreach (var player in Core.PlayerManager.GetAllValidPlayers())
        {
            SendPlayerConnectLogs(player);
            var team = GetTeamName(player);
            if (team != "Unassigned")
                SendLog(player, team, "joined team");
        }
        Console.WriteLine($"[HLstatsX:CE] Loaded. Sending logs to {_config.DaemonHost}:{_config.DaemonPort} (Server: {_config.ServerIp}:{_config.ServerPort}, ProxyKey: {_config.ProxyKey}, MaxPlayers: {_config.MaxPlayers}, ReceiverPort: {_config.ReceiverPort})");
    }

    public override void Unload()
    {
        try
        {
            _udpCts?.Cancel();
            _udpCts?.Dispose();
            _udpCts = null;
        }
        catch {}

        try
        {
            _udpReceiver?.Close();
            _udpReceiver?.Dispose();
            _udpReceiver = null;
        }
        catch {}

        CloseAllCustomHuds();
        _udpClient?.Close();
        _udpClient = null;
        Console.WriteLine("[HLstatsX:CE] Unloaded.");
    }

    private void StartUdpReceiver()
    {
        try
        {
            _udpCts = new System.Threading.CancellationTokenSource();
            _udpReceiver = new UdpClient(_config.ReceiverPort);
            var token = _udpCts.Token;

            System.Threading.Tasks.Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var res = await _udpReceiver.ReceiveAsync(token);
                        var raw = Encoding.UTF8.GetString(res.Buffer).Trim();
                        if (string.IsNullOrEmpty(raw)) continue;

                        if (raw.StartsWith("HLX_CMD ", StringComparison.OrdinalIgnoreCase))
                        {
                            var cmd = raw.Substring(8).Trim();
                            if (!string.IsNullOrEmpty(cmd))
                            {
                                Core.Scheduler.NextTick(() =>
                                {
                                    ExecuteDaemonCommand(cmd);
                                });
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HLstatsX:CE] UDP receive warning: {ex.Message}");
                    }
                }
            }, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Could not bind UDP receiver on port {_config.ReceiverPort}: {ex.Message}");
        }
    }

    private void ExecuteDaemonCommand(string rawCmd)
    {
        try
        {
            if (rawCmd.StartsWith("hlx_sm_msay ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawCmd.Substring(12).Trim().Split(' ', 3);
                if (parts.Length >= 3)
                {
                    string target = parts[0];
                    string text = parts[2].Trim('"', '\'').Replace("\\n", "\n").Replace("\r\n", "\n");
                    var player = FindPlayerTarget(target);
                    if (player != null && player.IsValid && !player.IsFakeClient)
                    {
                        player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, text));
                    }
                    else if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                            p.SendMessage(MessageType.Chat, FormatSourceModMessage(p, text));
                    }
                    return;
                }
            }

            if (rawCmd.StartsWith("hlx_sm_psay ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawCmd.Substring(12).Trim().Split(' ', 3);
                if (parts.Length >= 2)
                {
                    string target = parts[0];
                    string text = (parts.Length >= 3 ? parts[2] : parts[1]).Trim('"', '\'');
                    var player = FindPlayerTarget(target);
                    if (player != null && player.IsValid && !player.IsFakeClient)
                    {
                        player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, text));
                    }
                    else if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                            p.SendMessage(MessageType.Chat, FormatSourceModMessage(p, text));
                    }
                    return;
                }
            }

            Core.Engine.ExecuteCommand(rawCmd);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] ExecuteDaemonCommand error: {ex.Message}");
        }
    }

    private void SendUdpLog(string logLine)
    {
        try
        {
            if (_udpClient == null) return;
            var now = DateTime.Now;
            var formatted = $"L {now:MM/dd/yyyy} - {now:HH:mm:ss}: {logLine}\n";
            if (!string.IsNullOrEmpty(_config.ProxyKey) && !string.IsNullOrEmpty(_config.ServerIp))
            {
                formatted = $"PROXY Key={_config.ProxyKey} {_config.ServerIp}:{_config.ServerPort}PROXY {formatted}";
            }
            var data = Encoding.UTF8.GetBytes(formatted);
            _udpClient.Send(data, data.Length, _config.DaemonHost, _config.DaemonPort);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] UDP send error: {ex.Message}");
        }
    }

    private static string GetSteamId3(ulong steamId64)
    {
        if (steamId64 == 0 || steamId64 < 76561197960265729UL) return "BOT";
        uint accountId = (uint)(steamId64 - 76561197960265728UL);
        return $"[U:1:{accountId}]";
    }

    private static string GetTeamName(IPlayer player)
    {
        var controller = player.Controller;
        if (controller != null)
        {
            return controller.TeamNum switch
            {
                2 => "TERRORIST",
                3 => "CT",
                1 => "Spectator",
                _ => "Unassigned"
            };
        }
        return "Unassigned";
    }

    private static string CleanIp(string? rawIp)
    {
        if (string.IsNullOrWhiteSpace(rawIp) || rawIp.Equals("none", StringComparison.OrdinalIgnoreCase)) return "none";
        int colonIdx = rawIp.IndexOf(':');
        if (colonIdx > 0) return rawIp.Substring(0, colonIdx);
        return rawIp;
    }

    private void SendPlayerConnectLogs(IPlayer player)
    {
        if (player == null || !player.IsValid) return;
        var name = player.Name.Replace('"', '\'');
        var userid = player.UserID;
        var steam3 = GetSteamId3(player.SteamID);
        var ip = CleanIp(player.IPAddress);
        SendUdpLog($"\"{name}<{userid}><{steam3}><>\" connected, address \"{ip}\"");
        SendUdpLog($"\"{name}<{userid}><{steam3}><>\" STEAM USERID validated");
        SendUdpLog($"\"{name}<{userid}><{steam3}><>\" entered the game");
    }

    private static string FormatPlayerString(IPlayer player)
    {
        var name   = player.Name.Replace('"', '\'');
        var userid = player.UserID;
        var steam3 = GetSteamId3(player.SteamID);
        var team   = GetTeamName(player);
        return $"\"{name}<{userid}><{steam3}><{team}>\"";
    }

    private void SendLog(IPlayer? player, string message, string? verb)
    {
        if (player != null && player.IsValid && !string.IsNullOrWhiteSpace(verb))
        {
            var name   = player.Name.Replace('"', '\'');
            var userid = player.UserID;
            var steam3 = GetSteamId3(player.SteamID);
            var team   = GetTeamName(player);
            SendUdpLog($"\"{name}<{userid}><{steam3}><{team}>\" {verb} \"{message}\"");
        }
        else if (string.IsNullOrWhiteSpace(verb))
        {
            SendUdpLog(message);
        }
    }

    private IPlayer? FindPlayerTarget(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        token = token.Trim().Trim('"', '\'', '#');

        if (int.TryParse(token, out int uid))
        {
            var p = Core.PlayerManager.GetPlayer(uid);
            if (p != null && p.IsValid) return p;
            return Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(x => x.UserID == uid || x.Slot == uid);
        }

        if (ulong.TryParse(token, out ulong sid64) && sid64 > 76561197960265728UL)
            return Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(x => x.SteamID == sid64);

        return Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(x => x.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, WeaponStats> GetStatsmeDict(ulong steamId)
    {
        if (!_Statsme.TryGetValue(steamId, out var dict))
        {
            dict = new Dictionary<string, WeaponStats>(8);
            _Statsme[steamId] = dict;
        }
        return dict;
    }

    private static Dictionary<string, HitgroupStats> GetStatsme2Dict(ulong steamId)
    {
        if (!_Statsme2.TryGetValue(steamId, out var dict))
        {
            dict = new Dictionary<string, HitgroupStats>(8);
            _Statsme2[steamId] = dict;
        }
        return dict;
    }

    private void SendStatsme(IPlayer player, string statsVerb, Dictionary<string, string> props)
    {
        var sb = new StringBuilder();
        sb.Append(statsVerb);
        sb.Append('"');
        foreach (var kv in props)
        {
            sb.Append(" (");
            sb.Append(kv.Key);
            sb.Append(" \"");
            sb.Append(kv.Value);
            sb.Append("\")");
        }
        SendLog(player, sb.ToString(), "triggered");
    }

    private void FlushPlayerWeaponStats(IPlayer player)
    {
        if (player == null || !player.IsValid || player.IsFakeClient) return;
        var steamId = player.SteamID;

        if (_Statsme.TryGetValue(steamId, out var dict))
        {
            foreach (var kv in dict)
            {
                var s = kv.Value;
                SendStatsme(player, "weaponstats", new Dictionary<string, string>
                {
                    ["weapon"]    = kv.Key,
                    ["shots"]     = s.shots.ToString(),
                    ["hits"]      = s.hits.ToString(),
                    ["headshots"] = s.headshots.ToString(),
                    ["damage"]    = s.damage.ToString(),
                    ["kills"]     = s.kills.ToString(),
                    ["deaths"]    = s.deaths.ToString()
                });
            }
            _Statsme.Remove(steamId);
        }

        if (_Statsme2.TryGetValue(steamId, out var hdict))
        {
            foreach (var kv in hdict)
            {
                var hs = kv.Value;
                SendStatsme(player, "weaponstats2", new Dictionary<string, string>
                {
                    ["weapon"]   = kv.Key,
                    ["head"]     = hs.head.ToString(),
                    ["chest"]    = hs.chest.ToString(),
                    ["stomach"]  = hs.stomach.ToString(),
                    ["leftarm"]  = hs.leftarm.ToString(),
                    ["rightarm"] = hs.rightarm.ToString(),
                    ["leftleg"]  = hs.leftleg.ToString(),
                    ["rightleg"] = hs.rightleg.ToString()
                });
            }
            _Statsme2.Remove(steamId);
        }
    }

    private void RegisterServerCommands()
    {
        Core.Command.RegisterCommand("hlx_sm_psay", (context) =>
        {
            if (context.Args.Length < 2) return;
            string targetUserIdStr = context.Args[0];
            string message = context.Args.Length >= 3
                ? string.Join(" ", context.Args.Skip(2))
                : string.Join(" ", context.Args.Skip(1));

            if (targetUserIdStr.Equals("0") || targetUserIdStr.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, message));
            }
            else
            {
                foreach (var idStr in targetUserIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var player = FindPlayerTarget(idStr);
                    if (player != null && player.IsValid && !player.IsFakeClient)
                        player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, message));
                }
            }
        });

        Core.Command.RegisterCommand("hlx_sm_psay2", (context) =>
        {
            if (context.Args.Length < 2) return;
            var player = FindPlayerTarget(context.Args[0]);
            string message = string.Join(" ", context.Args.Skip(1));
            if (player != null && player.IsValid && !player.IsFakeClient)
                player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, message));
        });

        Core.Command.RegisterCommand("hlx_sm_csay", (context) =>
        {
            if (context.Args.Length < 1) return;
            string message = string.Join(" ", context.Args);
            foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                player.SendMessage(MessageType.Center, message);
        });

        Core.Command.RegisterCommand("hlx_sm_tsay", (context) =>
        {
            if (context.Args.Length < 2) return;
            string targetUserIdStr = context.Args.Length >= 3 ? context.Args[1] : context.Args[0];
            string message = context.Args.Length >= 3
                ? string.Join(" ", context.Args.Skip(2))
                : string.Join(" ", context.Args.Skip(1));

            if (targetUserIdStr.Equals("0") || targetUserIdStr.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    player.SendMessage(MessageType.Center, message);
            }
            else
            {
                var player = FindPlayerTarget(targetUserIdStr);
                if (player != null && player.IsValid && !player.IsFakeClient)
                    player.SendMessage(MessageType.Center, message);
            }
        });

        Core.Command.RegisterCommand("hlx_sm_hint", (context) =>
        {
            if (context.Args.Length < 2) return;
            string targetUserIdStr = context.Args[0];
            string message = string.Join(" ", context.Args.Skip(1));

            if (targetUserIdStr.Equals("0") || targetUserIdStr.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    player.SendMessage(MessageType.Alert, message);
            }
            else
            {
                var player = FindPlayerTarget(targetUserIdStr);
                if (player != null && player.IsValid && !player.IsFakeClient)
                    player.SendMessage(MessageType.Alert, message);
            }
        });

        Core.Command.RegisterCommand("hlx_sm_msay", (context) =>
        {
            if (context.Args.Length < 3) return;
            string targetUserIdStr = context.Args[0];
            string message = string.Join(" ", context.Args.Skip(2))
                .Replace("\\n", "\n").Replace("\r\n", "\n");
            string formattedMsg = FormatColors($"{_messagePrefix}{message}");

            if (targetUserIdStr.Equals("0") || targetUserIdStr.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    player.SendMessage(MessageType.Chat, formattedMsg);
            }
            else
            {
                var player = FindPlayerTarget(targetUserIdStr);
                if (player != null && player.IsValid && !player.IsFakeClient)
                    player.SendMessage(MessageType.Chat, formattedMsg);
            }
        });

        Core.Command.RegisterCommand("hlx_sm_swap", (context) =>
        {
            if (context.Args.Length < 1) return;
            var player = FindPlayerTarget(context.Args[0]);
            if (player != null && player.IsValid)
            {
                var teamNum = player.Controller?.TeamNum ?? 0;
                if (teamNum == 2) player.SwitchTeam(Team.CT);
                else if (teamNum == 3) player.SwitchTeam(Team.T);
            }
        });

        Core.Command.RegisterCommand("hlx_sm_player_action", (context) =>
        {
            if (context.Args.Length < 2) return;
            var player = FindPlayerTarget(context.Args[0]);
            if (player != null && player.IsValid)
                SendLog(player, context.Args[1], "triggered");
        });

        Core.Command.RegisterCommand("hlx_sm_team_action", (context) =>
        {
            if (context.Args.Length < 2) return;
            SendLog(null, $"Team \"{context.Args[0]}\" triggered \"{context.Args[1]}\"", null);
        });

        Core.Command.RegisterCommand("hlx_sm_world_action", (context) =>
        {
            if (context.Args.Length < 1) return;
            SendLog(null, $"World triggered \"{context.Args[0]}\"", null);
        });

        Core.Command.RegisterCommand("hlx_protect_address", (context) =>
        {
            if (context.Args.Length > 0)
                _protectAddress = context.Args[0];
        });

        Core.Command.RegisterCommand("hlx_block_commands", (context) =>
        {
            if (context.Args.Length > 0 && int.TryParse(context.Args[0], out int val))
                _blockChatCommands = (val == 1);
        });

        Core.Command.RegisterCommand("hlx_message_prefix", (context) =>
        {
            if (context.Args.Length > 0)
                _messagePrefix = string.Join(" ", context.Args);
        });

        Core.Command.RegisterCommand("hlx_message_prefix_clear", (context) =>
        {
            _messagePrefix = "";
        });

        void RegisterPlayerStatsCommand(string name)
        {
            Core.Command.RegisterCommand(name, (context) =>
            {
                if (!context.IsSentByPlayer || context.Sender is not { } player || !player.IsValid) return;
                string arg = context.Args.Length > 0 ? " " + string.Join(" ", context.Args) : "";
                SendLog(player, $"{name}{arg}", "say");
            });
        }

        RegisterPlayerStatsCommand("rank");
        RegisterPlayerStatsCommand("skill");
        RegisterPlayerStatsCommand("points");
        RegisterPlayerStatsCommand("place");
        RegisterPlayerStatsCommand("top10");
        RegisterPlayerStatsCommand("top20");
        RegisterPlayerStatsCommand("top5");
        RegisterPlayerStatsCommand("statsme");
        RegisterPlayerStatsCommand("session");
        RegisterPlayerStatsCommand("session_data");
        RegisterPlayerStatsCommand("next");
        RegisterPlayerStatsCommand("kpd");
        RegisterPlayerStatsCommand("kdratio");
        RegisterPlayerStatsCommand("kdeath");
        RegisterPlayerStatsCommand("weapons");
        RegisterPlayerStatsCommand("accuracy");
        RegisterPlayerStatsCommand("targets");
        RegisterPlayerStatsCommand("kills");
        RegisterPlayerStatsCommand("servers");
        RegisterPlayerStatsCommand("hlx");
        RegisterPlayerStatsCommand("hlstatsx");
        RegisterPlayerStatsCommand("menu");
    }

    private void RegisterGameEvents()
    {
        Core.GameEvent.HookPre<EventPlayerChat>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;

            string text = @event.Text?.Trim() ?? "";
            string cleaned = text;
            if (cleaned.StartsWith('/') || cleaned.StartsWith('!'))
                cleaned = cleaned.Substring(1).Trim();

            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                string cmd = parts[0];
                if (MenuCommands.Contains(cmd))
                {
                    OpenStatsMenu(player);
                    return HookResult.Handled;
                }

                if (BlockedCommands.Contains(cmd))
                {
                    string verb = @event.TeamOnly ? "say_team" : "say";
                    SendLog(player, text, verb);
                    if (_blockChatCommands) return HookResult.Handled;
                    return HookResult.Continue;
                }
            }

            string chatVerb = @event.TeamOnly ? "say_team" : "say";
            SendLog(player, text, chatVerb);
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventRoundEnd>((@event) =>
        {
            foreach (var player in Core.PlayerManager.GetAllValidPlayers())
                FlushPlayerWeaponStats(player);
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventRoundMvp>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;

            string reasonText = @event.Reason switch
            {
                1  => "with most eliminations",
                2  => "with bomb planted",
                3  => "with bomb defused",
                4  => "with hostage rescued",
                11 => "with HE grenade",
                14 => "with a clutch defuse",
                15 => "with most kills",
                _  => "with best overall"
            };

            SendLog(player, $"round_mvp {reasonText}", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombBeginplant>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Planted_The_Bomb", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombAbortplant>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Plant_Aborted", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombPlanted>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Planted_The_Bomb", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombBegindefuse>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid)
            {
                var action = @event.HasKit ? "Begin_Bomb_Defuse_With_Kit" : "Begin_Bomb_Defuse_Without_Kit";
                SendLog(player, action, "triggered");
            }
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombAbortdefuse>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Defuse_Aborted", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombDefused>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Defused_The_Bomb", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombDropped>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Dropped_The_Bomb", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombPickup>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Got_The_Bomb", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventBombExploded>((@event) =>
        {
            SendUdpLog("World triggered \"Target_Bombed\"");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventRoundStart>((@event) =>
        {
            SendUdpLog("World triggered \"Round_Start\"");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventRoundFreezeEnd>((@event) =>
        {
            SendUdpLog("World triggered \"Round_Freeze_End\"");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventCsWinPanelRound>((@event) =>
        {
            if (@event.FinalEvent == 8)
                SendUdpLog("Team \"CT\" triggered \"SFUI_Notice_Bomb_Defused\"");
            else if (@event.FinalEvent == 9)
                SendUdpLog("Team \"TERRORIST\" triggered \"SFUI_Notice_Target_Bombed\"");
            else if (@event.FinalEvent == 10)
                SendUdpLog("Team \"TERRORIST\" triggered \"SFUI_Notice_Terrorists_Win\"");
            else if (@event.FinalEvent == 7)
                SendUdpLog("Team \"CT\" triggered \"SFUI_Notice_CTs_Win\"");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventHostageFollows>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Touched_A_Hostage", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventHostageRescued>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Rescued_A_Hostage", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventHostageRescuedAll>((@event) =>
        {
            SendUdpLog("Team \"CT\" triggered \"SFUI_Notice_All_Hostages_Rescued\"");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventHostageKilled>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid) SendLog(player, "Killed_A_Hostage", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerBlind>((@event) =>
        {
            var attacker = @event.AttackerPlayer;
            var victim   = @event.UserIdPlayer;
            if (attacker != null && attacker.IsValid && victim != null && victim.IsValid && attacker.UserID != victim.UserID)
                SendUdpLog($"{FormatPlayerString(attacker)} triggered \"blinded\" against {FormatPlayerString(victim)}");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerFalldamage>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient && @event.Damage > 0)
                SendLog(player, $"falldamage (damage \"{(int)@event.Damage}\")", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventGrenadeThrown>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
            {
                var weapon = @event.Weapon ?? "";
                if (weapon.StartsWith("weapon_")) weapon = weapon.Substring(7);
                SendLog(player, $"throw_grenade (weapon \"{weapon}\")", "triggered");
            }
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventHegrenadeDetonate>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "hegrenade_detonate", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventFlashbangDetonate>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "flashbang_detonate", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventSmokegrenadeDetonate>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "smokegrenade_detonate", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventMolotovDetonate>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "molotov_detonate", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventDecoyStarted>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "decoy_started", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventDecoyDetonate>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid && !player.IsFakeClient)
                SendLog(player, "decoy_detonate", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerSpawn>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player != null && player.IsValid)
                SendLog(player, "spawned", "triggered");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerTeam>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            string team = @event.Team switch
            {
                2 => "TERRORIST",
                3 => "CT",
                1 => "Spectator",
                _ => "Unassigned"
            };
            SendLog(player, team, "joined team");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerChangename>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            var newName = @event.NewName?.Replace('"', '\'') ?? "";
            if (!string.IsNullOrEmpty(newName))
                SendLog(player, newName, "changed name to");
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerConnect>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            SendPlayerConnectLogs(player);
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerConnectFull>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            SendPlayerConnectLogs(player);
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerDisconnect>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            FlushPlayerWeaponStats(player);
            var name   = player.Name.Replace('"', '\'');
            var userid = player.UserID;
            var steam3 = GetSteamId3(player.SteamID);
            var team   = GetTeamName(player);
            string reason = @event.Reason switch
            {
                1  => "Disconnected",
                5  => "Kicked",
                _  => "Disconnected"
            };
            SendUdpLog($"\"{name}<{userid}><{steam3}><{team}>\" disconnected (reason \"{reason}\")");
            CloseCustomHud(player.PlayerID);
            return HookResult.Continue;
        });

        Core.Event.OnMapLoad += (@event) =>
        {
            CloseAllCustomHuds();
            var mapName = @event.MapName ?? "";
            SendUdpLog($"server_cvar: \"maxplayers\" \"{_config.MaxPlayers}\"");
            SendUdpLog($"Loading map \"{mapName}\"");
            SendUdpLog($"Started map \"{mapName}\" (CRC \"-1\")");
        };

        Core.GameEvent.HookPost<EventWeaponFire>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid || player.IsFakeClient) return HookResult.Continue;
            var weapon = @event.Weapon ?? "";
            if (weapon.StartsWith("weapon_")) weapon = weapon.Substring(7);
            if (!WeaponCode.TryGetValue(weapon, out var wCode)) return HookResult.Continue;
            var dict = GetStatsmeDict(player.SteamID);
            if (!dict.TryGetValue(wCode, out var stats)) stats = default;
            stats.shots++;
            dict[wCode] = stats;
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerHurt>((@event) =>
        {
            var attacker = @event.AttackerPlayer;
            var victim   = @event.UserIdPlayer;
            var weapon = @event.Weapon ?? "";
            if (weapon.StartsWith("weapon_")) weapon = weapon.Substring(7);
            string wCode = WeaponCode.TryGetValue(weapon, out var mapped) ? mapped : weapon;

            if (attacker != null && attacker.IsValid && victim != null && victim.IsValid)
            {
                string hitgroupName = ((int)@event.ActualHitGroup) switch
                {
                    1 => "head",
                    2 => "chest",
                    3 => "stomach",
                    4 => "left arm",
                    5 => "right arm",
                    6 => "left leg",
                    7 => "right leg",
                    _ => "generic"
                };
                int remainHealth = @event.ActualHealth;
                int remainArmor = @event.ActualArmor;
                SendUdpLog($"{FormatPlayerString(attacker)} attacked {FormatPlayerString(victim)} with \"{wCode}\" (damage \"{@event.ActualDmgHealth}\") (damage_armor \"{@event.ActualDmgArmor}\") (health \"{remainHealth}\") (armor \"{remainArmor}\") (hitgroup \"{hitgroupName}\")");
            }

            if (attacker != null && attacker.IsValid && !attacker.IsFakeClient)
            {
                var dict = GetStatsmeDict(attacker.SteamID);
                if (!dict.TryGetValue(wCode, out var stats)) stats = default;
                stats.hits++;
                stats.damage += @event.ActualDmgHealth;
                int hitgroup = (int)@event.ActualHitGroup;
                if (hitgroup == 1) stats.headshots++;
                dict[wCode] = stats;

                var hdict = GetStatsme2Dict(attacker.SteamID);
                if (!hdict.TryGetValue(wCode, out var hstats)) hstats = default;
                switch (hitgroup)
                {
                    case 1: hstats.head++;     break;
                    case 2: hstats.chest++;    break;
                    case 3: hstats.stomach++;  break;
                    case 4: hstats.leftarm++;  break;
                    case 5: hstats.rightarm++; break;
                    case 6: hstats.leftleg++;  break;
                    case 7: hstats.rightleg++; break;
                }
                hdict[wCode] = hstats;
            }
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerDeath>((@event) =>
        {
            var attacker = @event.AttackerPlayer;
            var victim   = @event.UserIdPlayer;
            var weapon   = @event.Weapon ?? "";
            if (weapon.StartsWith("weapon_")) weapon = weapon.Substring(7);
            string finalWeapon = WeaponCode.TryGetValue(weapon, out var mapped) ? mapped : weapon;

            if (victim != null && victim.IsValid)
            {
                if (attacker != null && attacker.IsValid && attacker.UserID != victim.UserID)
                {
                    string headshotProp = @event.Headshot ? " (headshot)" : "";
                    SendUdpLog($"{FormatPlayerString(attacker)} killed {FormatPlayerString(victim)} with \"{finalWeapon}\"{headshotProp}");
                }
                else if (attacker == null || attacker.UserID == victim.UserID)
                {
                    SendLog(victim, finalWeapon, "committed suicide with");
                }

                if (@event.AssisterPlayer != null && @event.AssisterPlayer.IsValid && !@event.AssisterPlayer.IsFakeClient)
                    SendUdpLog($"{FormatPlayerString(@event.AssisterPlayer)} triggered \"assisted_kill\" against {FormatPlayerString(victim)}");

                if (@event.Dominated > 0 && attacker != null && attacker.IsValid)
                    SendUdpLog($"{FormatPlayerString(attacker)} triggered \"domination\" against {FormatPlayerString(victim)}");

                if (@event.Revenge > 0 && attacker != null && attacker.IsValid)
                    SendUdpLog($"{FormatPlayerString(attacker)} triggered \"revenge\" against {FormatPlayerString(victim)}");
            }

            if (!string.IsNullOrEmpty(finalWeapon) && WeaponCode.ContainsKey(finalWeapon))
            {
                if (attacker != null && attacker.IsValid && !attacker.IsFakeClient)
                {
                    var dict = GetStatsmeDict(attacker.SteamID);
                    if (!dict.TryGetValue(finalWeapon, out var stats)) stats = default;
                    stats.kills++;
                    dict[finalWeapon] = stats;
                }
                if (victim != null && victim.IsValid && !victim.IsFakeClient)
                {
                    var dict = GetStatsmeDict(victim.SteamID);
                    if (!dict.TryGetValue(finalWeapon, out var stats)) stats = default;
                    stats.deaths++;
                    dict[finalWeapon] = stats;
                    FlushPlayerWeaponStats(victim);
                }
            }
            return HookResult.Continue;
        });

        Core.Event.OnCustomHudClicked += HandleCustomHudClick;
    }

    private static string FormatColors(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("[default]",     "\x01", StringComparison.OrdinalIgnoreCase)
            .Replace("[white]",       "\x01", StringComparison.OrdinalIgnoreCase)
            .Replace("[darkred]",     "\x02", StringComparison.OrdinalIgnoreCase)
            .Replace("[team]",        "\x03", StringComparison.OrdinalIgnoreCase)
            .Replace("[green]",       "\x04", StringComparison.OrdinalIgnoreCase)
            .Replace("[lightgreen]",  "\x05", StringComparison.OrdinalIgnoreCase)
            .Replace("[lime]",        "\x06", StringComparison.OrdinalIgnoreCase)
            .Replace("[red]",         "\x07", StringComparison.OrdinalIgnoreCase)
            .Replace("[lightred]",    "\x07", StringComparison.OrdinalIgnoreCase)
            .Replace("[grey]",        "\x08", StringComparison.OrdinalIgnoreCase)
            .Replace("[gray]",        "\x08", StringComparison.OrdinalIgnoreCase)
            .Replace("[yellow]",      "\x09", StringComparison.OrdinalIgnoreCase)
            .Replace("[lightyellow]", "\x09", StringComparison.OrdinalIgnoreCase)
            .Replace("[silver]",      "\x0A", StringComparison.OrdinalIgnoreCase)
            .Replace("[blue]",        "\x0B", StringComparison.OrdinalIgnoreCase)
            .Replace("[darkblue]",    "\x0C", StringComparison.OrdinalIgnoreCase)
            .Replace("[purple]",      "\x0D", StringComparison.OrdinalIgnoreCase)
            .Replace("[magenta]",     "\x0E", StringComparison.OrdinalIgnoreCase)
            .Replace("[gold]",        "\x10", StringComparison.OrdinalIgnoreCase)
            .Replace("[orange]",      "\x10", StringComparison.OrdinalIgnoreCase)
            .Replace("{DEFAULT}",     "\x01", StringComparison.OrdinalIgnoreCase)
            .Replace("{RED}",         "\x07", StringComparison.OrdinalIgnoreCase)
            .Replace("{TEAM}",        "\x03", StringComparison.OrdinalIgnoreCase)
            .Replace("{GREEN}",       "\x04", StringComparison.OrdinalIgnoreCase)
            .Replace("{LIME}",        "\x06", StringComparison.OrdinalIgnoreCase)
            .Replace("{LIGHTRED}",    "\x07", StringComparison.OrdinalIgnoreCase)
            .Replace("{GRAY}",        "\x08", StringComparison.OrdinalIgnoreCase)
            .Replace("{GREY}",        "\x08", StringComparison.OrdinalIgnoreCase)
            .Replace("{YELLOW}",      "\x09", StringComparison.OrdinalIgnoreCase)
            .Replace("{BLUE}",        "\x0B", StringComparison.OrdinalIgnoreCase)
            .Replace("{DARKBLUE}",    "\x0C", StringComparison.OrdinalIgnoreCase)
            .Replace("{PURPLE}",      "\x0D", StringComparison.OrdinalIgnoreCase)
            .Replace("{GOLD}",        "\x10", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Regex KillRewardRegex = new(
        @"^(?<killer>.+?)\s*\((?<kpts>[\d,]+)\)(?<kextra>.*?)\s*got\s*(?<pts>[+-]?\d+)\s*points(?<vextra>.*?)\s*for killing\s*(?<victim>.+?)\s*\((?<vpts>[\d,]+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex KillRewardSimpleRegex = new(
        @"^(?<killer>.+?)\s*\((?<kpts>[\d,]+)\)(?<kextra>.*?)\s*got\s*(?<pts>[+-]?\d+)\s*points\s*for killing\s*(?<victim>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ActionRewardRegex = new(
        @"^(?<team>.+?)\s+(?<verb>got|lost)\s+(?<pts>[\d,]+)\s+points\s+for\s+(?<action>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TeamkillPenaltyRegex = new(
        @"^(?<killer>.+?)\s*lost\s*(?<pts>[\d,]+)\s*points\s*\((?<total>[\d,]+)\)\s*for team-killing",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RankMsgRegex = new(
        @"^(?<player>.+?)\s*is on rank\s*#?(?<rank>\d+)\s*of\s*(?<total>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)!?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RankHiddenMsgRegex = new(
        @"^(?<player>.+?)\s*is on rank\s*\(HIDDEN\)\s*of\s*(?<total>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)!?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string FormatSourceModMessage(IPlayer player, string rawMsg)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawMsg)) return "";
            var loc = Core.Translation.GetPlayerLocalizer(player);

            var m = KillRewardRegex.Match(rawMsg);
            if (m.Success)
            {
                string tmpl = loc["hlx.kill_reward"] ?? "[green][HLstatsX][default] [yellow]{0}[default] ({1}) got [lime]+{2} points[default] for killing [yellow]{3}[default] ({4})!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["kpts"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["victim"].Value.Trim(), m.Groups["vpts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = KillRewardSimpleRegex.Match(rawMsg);
            if (m.Success)
            {
                string tmpl = loc["hlx.kill_reward"] ?? "[green][HLstatsX][default] [yellow]{0}[default] ({1}) got [lime]+{2} points[default] for killing [yellow]{3}[default] ({4})!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["kpts"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["victim"].Value.Trim(), "-");
                return FormatColors(formatted);
            }

            m = TeamkillPenaltyRegex.Match(rawMsg);
            if (m.Success)
            {
                string tmpl = loc["hlx.teamkill_penalty"] ?? "[green][HLstatsX][default] [yellow]{0}[default] lost [red]-{1} points[default] ({2}) for team-killing!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["total"].Value.Trim());
                return FormatColors(formatted);
            }

            m = ActionRewardRegex.Match(rawMsg);
            if (m.Success)
            {
                string verb = m.Groups["verb"].Value.ToLowerInvariant();
                string key = verb == "lost" ? "hlx.points_lost" : "hlx.points_got";
                string tmpl = loc[key] ?? (verb == "lost"
                    ? "[green][HLstatsX][default] You [red]lost -{0} points[default] ({1}) for [yellow]{2}[default]!"
                    : "[green][HLstatsX][default] You [lime]got +{0} points[default] ({1}) for [yellow]{2}[default]!");
                string formatted = string.Format(tmpl, m.Groups["pts"].Value.Trim(), m.Groups["team"].Value.Trim(), m.Groups["action"].Value.Trim());
                return FormatColors(formatted);
            }

            m = RankMsgRegex.Match(rawMsg);
            if (m.Success)
            {
                string tmpl = loc["hlx.rank"] ?? "[green][HLstatsX][default] [yellow]{0}[default] is on rank [lightred]#{1}[default] of [lightred]{2}[default] with [lightred]{3}[default] points!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["rank"].Value.Trim(), m.Groups["total"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = RankHiddenMsgRegex.Match(rawMsg);
            if (m.Success)
            {
                string tmpl = loc["hlx.rank_hidden"] ?? "[green][HLstatsX][default] [yellow]{0}[default] is on rank [grey](HIDDEN)[default] of [lightred]{1}[default] with [lightred]{2}[default] points!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["total"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] FormatSourceModMessage error: {ex.Message}");
        }

        return FormatColors($"{_messagePrefix}{rawMsg}");
    }

    private void CancelMenuAutoClose(int playerId)
    {
        if (_menuCloseTimers.TryGetValue(playerId, out var cts))
        {
            try { cts?.Cancel(); } catch {}
            _menuCloseTimers.Remove(playerId);
        }
    }

    private void ScheduleMenuAutoClose(IPlayer player)
    {
        if (player == null || !player.IsValid) return;
        int playerId = player.PlayerID;
        CancelMenuAutoClose(playerId);

        if (_config.AutoCloseMenuSeconds > 0)
        {
            var cts = Core.Scheduler.DelayBySeconds(_config.AutoCloseMenuSeconds, () =>
            {
                CloseCustomHud(playerId);
                try { Core.MenusAPI.CloseActiveMenu(player); } catch {}
                _menuCloseTimers.Remove(playerId);
            });
            _menuCloseTimers[playerId] = cts;
        }
    }

    private void CloseAllCustomHuds()
    {
        try
        {
            foreach (var kvp in _menuCloseTimers)
            {
                try { kvp.Value?.Cancel(); } catch {}
            }
            _menuCloseTimers.Clear();


            foreach (var kvp in _playerHuds)
            {
                var playerId = kvp.Key;
                var hud = kvp.Value;
                if (hud != null && hud.IsValid)
                {
                    hud.SetInputCaptureEnabledForPlayer(playerId, false);
                    SetHudClass(hud, "HlxMenuPanel", "Visible", false);
                    hud.Despawn();
                }
            }
            _playerHuds.Clear();
            _activeHudPlayers.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CloseAllCustomHuds error: {ex.Message}");
        }
    }

    private void CloseCustomHud(int playerId)
    {
        try
        {
            CancelMenuAutoClose(playerId);
            _activeHudPlayers.Remove(playerId);
            if (_playerHuds.TryGetValue(playerId, out var hud))
            {
                if (hud != null && hud.IsValid)
                {
                    hud.SetInputCaptureEnabledForPlayer(playerId, false);
                    SetHudClass(hud, "HlxMenuPanel", "Visible", false);
                    hud.Despawn();
                }
                _playerHuds.Remove(playerId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CloseCustomHud error: {ex.Message}");
        }
    }

    private static void SetHudClass(CCSCustomHudLayout hud, string panelId, string className, bool hasClass)
    {
        hud.SetHasClass(panelId, className, hasClass
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);
    }

    private void OpenStatsMenu(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        ScheduleMenuAutoClose(player);

        if (_config.MenuType.Equals("CustomHud", StringComparison.OrdinalIgnoreCase))
        {
            OpenCustomHudMenu(player);
            return;
        }

        OpenBuiltinMenu(player);
    }

    private void OpenCustomHudMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            CCSCustomHudLayout hud;

            if (_playerHuds.TryGetValue(playerId, out var existingHud) && existingHud != null && existingHud.IsValid)
            {
                hud = existingHud;
            }
            else
            {
                hud = Core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
                hud.StrLayout = _config.CustomMenuLayout;
                hud.StrLayoutUpdated();
                hud.DispatchSpawn();

                hud.SetTransmitState(false);
                hud.SetTransmitState(true, playerId);
                _playerHuds[playerId] = hud;
            }

            var loc = Core.Translation.GetPlayerLocalizer(player);
            string title = loc["hlx.menu_title"] ?? "► HLstatsX:CE Stats";

            hud.SetDialogVariableString("HlxMenuTitle", "menu_title", title);
            hud.SetDialogVariableString("HlxMenuOption01Label", "option_01", "1. My Rank");
            hud.SetDialogVariableString("HlxMenuOption02Label", "option_02", "2. Top 10");
            hud.SetDialogVariableString("HlxMenuOption03Label", "option_03", "3. Top 20");
            hud.SetDialogVariableString("HlxMenuOption04Label", "option_04", "4. Next Above Me");
            hud.SetDialogVariableString("HlxMenuOption05Label", "option_05", "5. My Session");
            hud.SetDialogVariableString("HlxMenuOption06Label", "option_06", "6. My Stats");
            hud.SetDialogVariableString("HlxMenuOption07Label", "option_07", "7. Weapon Stats");

            hud.SetDialogVariableString("HlxMenuBackLabel", "back_text", "7. Back");
            hud.SetDialogVariableString("HlxMenuNextLabel", "next_text", "8. Next");
            hud.SetDialogVariableString("HlxMenuExitLabel", "exit_text", "9. Exit");

            SetHudClass(hud, "HlxMenuBack", "Hidden", true);
            SetHudClass(hud, "HlxMenuNext", "Hidden", true);
            SetHudClass(hud, "HlxMenuExit", "Hidden", false);

            SetHudClass(hud, "HlxMenuOption01", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption02", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption03", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption04", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption05", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption06", "Interactive", true);
            SetHudClass(hud, "HlxMenuOption07", "Interactive", true);
            SetHudClass(hud, "HlxMenuExit", "Interactive", true);

            hud.SetInputCaptureEnabledForPlayer(playerId, true);
            SetHudClass(hud, "HlxMenuPanel", "Visible", true);
            _activeHudPlayers.Add(playerId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CustomHud menu error: {ex.Message}. Falling back to BuiltIn menu.");
            OpenBuiltinMenu(player);
        }
    }

    private void HandleCustomHudClick(IOnCustomHudClickedEvent @event)
    {
        try
        {
            int playerId = @event.PlayerId;
            if (!_activeHudPlayers.Contains(playerId)) return;
            if (!_playerHuds.TryGetValue(playerId, out var hud) || hud == null || !hud.IsValid) return;

            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player == null || !player.IsValid) return;

            string button = @event.ButtonId ?? "";

            switch (button)
            {
                case "HlxMenuOption01":
                    CloseCustomHud(playerId);
                    SendLog(player, "rank", "say");
                    break;
                case "HlxMenuOption02":
                    CloseCustomHud(playerId);
                    SendLog(player, "top10", "say");
                    break;
                case "HlxMenuOption03":
                    CloseCustomHud(playerId);
                    SendLog(player, "top20", "say");
                    break;
                case "HlxMenuOption04":
                    CloseCustomHud(playerId);
                    SendLog(player, "next", "say");
                    break;
                case "HlxMenuOption05":
                    CloseCustomHud(playerId);
                    SendLog(player, "session", "say");
                    break;
                case "HlxMenuOption06":
                    CloseCustomHud(playerId);
                    SendLog(player, "statsme", "say");
                    break;
                case "HlxMenuOption07":
                    CloseCustomHud(playerId);
                    SendLog(player, "weapons", "say");
                    break;
                case "HlxMenuExit":
                    CloseCustomHud(playerId);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] HandleCustomHudClick error: {ex.Message}");
        }
    }

    private void OpenBuiltinMenu(IPlayer player)
    {
        try
        {
            var builder = Core.MenusAPI.CreateBuilder();
            builder.Design.SetMenuTitle("► HLstatsX:CE Stats");
            builder.Design.SetMenuTitleVisible(true);
            builder.Design.SetMenuTitleItemCountVisible(false);
            builder.Design.SetMenuFooterVisible(true);
            builder.Design.SetDefaultComment("Select an option to view your stats");

            builder.AddOption(new HlxMenuOption("My Rank",       "rank",    player, this));
            builder.AddOption(new HlxMenuOption("Top 10",        "top10",   player, this));
            builder.AddOption(new HlxMenuOption("Top 20",        "top20",   player, this));
            builder.AddOption(new HlxMenuOption("Next Above Me", "next",    player, this));
            builder.AddOption(new HlxMenuOption("My Session",    "session", player, this));
            builder.AddOption(new HlxMenuOption("My Stats",      "statsme", player, this));
            builder.AddOption(new HlxMenuOption("Weapon Stats",  "weapons", player, this));
            builder.AddOption(new HlxMenuOption("Server List",   "servers", player, this));

            Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Menu error: {ex.Message}");
            player.SendMessage(MessageType.Chat,
                FormatColors("[gold][HLstatsX:CE][default] Commands: [green]!rank  !top10  !session  !statsme  !next  !weapons"));
        }
    }

#pragma warning disable CS0067
    internal sealed class HlxMenuOption : IMenuOption, IDisposable
    {
        private readonly string    _label;
        private readonly string    _command;
        private readonly IPlayer   _targetPlayer;
        private readonly HLStatsX  _plugin;

        public HlxMenuOption(string label, string command, IPlayer targetPlayer, HLStatsX plugin)
        {
            _label        = label;
            _command      = command;
            _targetPlayer = targetPlayer;
            _plugin       = plugin;
        }

        public IMenuAPI? Menu { get; set; }
        public int LineCount => 1;
        public string Text  { get => _label; set { } }
        public string Comment { get => ""; set { } }
        public float MaxWidth { get => 0f; set { } }
        public bool Visible   { get => true; set { } }
        public bool Enabled   { get => true; set { } }
        public bool CloseAfterClick => true;
        public object? Tag { get; set; }
        public MenuOptionTextSize  TextSize  { get; set; } = MenuOptionTextSize.Medium;
        public MenuOptionTextStyle TextStyle { get; set; } = MenuOptionTextStyle.TruncateEnd;
        public bool PlaySound { get; set; } = true;

        public event EventHandler<MenuOptionEventArgs>?           VisibilityChanged;
        public event EventHandler<MenuOptionEventArgs>?           EnabledChanged;
        public event EventHandler<MenuOptionEventArgs>?           TextChanged;
        public event EventHandler<MenuOptionValidatingEventArgs>? Validating;
        public event AsyncEventHandler<MenuOptionClickEventArgs>? Click;
        public event EventHandler<MenuOptionFormattingEventArgs>? BeforeFormat;
        public event EventHandler<MenuOptionFormattingEventArgs>? AfterFormat;

        public bool   IsClickTaskCompleted(IPlayer player)            => true;
        public bool   GetVisible(IPlayer player)                      => true;
        public void   SetVisible(IPlayer player, bool visible)        { }
        public bool   GetEnabled(IPlayer player)                      => true;
        public void   SetEnabled(IPlayer player, bool enabled)        { }
        public string GetDisplayText(IPlayer player, int displayLine) => _label;

        public System.Threading.Tasks.ValueTask<bool> OnValidatingAsync(IPlayer player)
            => new System.Threading.Tasks.ValueTask<bool>(true);

        public async System.Threading.Tasks.ValueTask OnClickAsync(IPlayer player)
        {
            _plugin.CancelMenuAutoClose(player.PlayerID);
            _plugin.SendLog(player, _command, "say");
            await System.Threading.Tasks.ValueTask.CompletedTask;
        }

        public void Dispose() { }
    }
#pragma warning restore CS0067
}