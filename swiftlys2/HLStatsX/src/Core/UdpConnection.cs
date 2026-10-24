using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
    private UdpClient? _udpClient;
    private UdpClient? _udpReceiver;
    private CancellationTokenSource? _udpCts;
    private Task? _udpReceiveTask;

    private void EnsureUdpConnection()
    {
        try
        {
            if (_udpClient == null)
            {
                _udpClient = new UdpClient();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Failed to create UDP sender: {ex.Message}");
        }

        if (_udpReceiver == null || _udpCts == null || _udpCts.IsCancellationRequested || (_udpReceiveTask != null && _udpReceiveTask.IsCompleted))
        {
            StartUdpReceiver();
        }
    }

    private void RestartUdpConnection()
    {
        StopUdpConnection();
        EnsureUdpConnection();
    }

    private void StartUdpReceiver()
    {
        try
        {
            try { _udpCts?.Cancel(); _udpCts?.Dispose(); } catch { }
            try { _udpReceiver?.Close(); _udpReceiver?.Dispose(); } catch { }

            _udpCts = new CancellationTokenSource();
            _udpReceiver = new UdpClient();
            _udpReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpReceiver.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _config.ReceiverPort));

            var token = _udpCts.Token;
            var receiver = _udpReceiver;
            _udpReceiveTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var res = await receiver.ReceiveAsync(token);
                        var raw = Encoding.UTF8.GetString(res.Buffer).Trim();
                        if (string.IsNullOrEmpty(raw)) continue;
                        LogDebug($"UDP RX from {res.RemoteEndPoint}: {raw}");
                        if (raw.StartsWith("HLX_CMD ", StringComparison.OrdinalIgnoreCase))
                        {
                            var cmd = raw.Substring(8).Trim();
                            if (!string.IsNullOrEmpty(cmd))
                                Core.Scheduler.NextTick(() => ExecuteDaemonCommand(cmd));
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            Console.WriteLine($"[HLstatsX:CE] UDP receive warning: {ex.Message}");
                    }
                }
            }, token);
            Console.WriteLine($"[HLstatsX:CE] Listening for daemon commands on UDP port {_config.ReceiverPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Could not bind UDP receiver on port {_config.ReceiverPort}: {ex.Message}");
        }
    }

    private void StopUdpConnection()
    {
        try { _udpCts?.Cancel(); _udpCts?.Dispose(); _udpCts = null; } catch { }
        try { _udpReceiver?.Close(); _udpReceiver?.Dispose(); _udpReceiver = null; } catch { }
        try { _udpClient?.Close(); _udpClient?.Dispose(); _udpClient = null; } catch { }
        _udpReceiveTask = null;
    }

    private static string ExtractNextToken(string input, ref int pos)
    {
        while (pos < input.Length && char.IsWhiteSpace(input[pos])) pos++;
        if (pos >= input.Length) return "";

        if (input[pos] == '"' || input[pos] == '\'')
        {
            char q = input[pos++];
            int endQ = input.IndexOf(q, pos);
            if (endQ == -1)
            {
                string val = input.Substring(pos);
                pos = input.Length;
                return val;
            }
            else
            {
                string val = input.Substring(pos, endQ - pos);
                pos = endQ + 1;
                return val;
            }
        }
        else
        {
            int start = pos;
            while (pos < input.Length && !char.IsWhiteSpace(input[pos])) pos++;
            return input.Substring(start, pos - start);
        }
    }

    private void HandleMsayPayload(string msayPayload)
    {
        /* Syntax from HLstatsX daemon: hlx_sm_msay <duration> <userid> [need_handler] <message> */
        if (string.IsNullOrWhiteSpace(msayPayload)) return;

        int pos = 0;
        string tok1 = ExtractNextToken(msayPayload, ref pos);
        string tok2 = ExtractNextToken(msayPayload, ref pos);
        if (string.IsNullOrEmpty(tok1) || string.IsNullOrEmpty(tok2)) return;

        int duration = 15;
        string target = tok2;
        if (int.TryParse(tok1, out int d1) && d1 > 0)
        {
            duration = d1;
            target = tok2;
        }
        else if (int.TryParse(tok2, out int d2) && d2 > 0)
        {
            target = tok1;
            duration = d2;
        }

        int savedPos = pos;
        string tok3 = ExtractNextToken(msayPayload, ref pos);
        string message;

        /* Check if 3rd token is optional need_handler flag (0 or 1) followed by the message */
        int afterTok3 = pos;
        while (afterTok3 < msayPayload.Length && char.IsWhiteSpace(msayPayload[afterTok3])) afterTok3++;
        if ((tok3 == "0" || tok3 == "1") && afterTok3 < msayPayload.Length)
        {
            message = msayPayload.Substring(afterTok3).Trim();
        }
        else
        {
            while (savedPos < msayPayload.Length && char.IsWhiteSpace(msayPayload[savedPos])) savedPos++;
            message = savedPos < msayPayload.Length ? msayPayload.Substring(savedPos).Trim() : "";
        }

        if (message.Length >= 2 && ((message.StartsWith('"') && message.EndsWith('"')) || (message.StartsWith('\'') && message.EndsWith('\''))))
        {
            message = message.Substring(1, message.Length - 2);
        }

        message = message.Replace("\\n", "\n").Replace("\r\n", "\n");
        if (string.IsNullOrWhiteSpace(message)) return;

        var player = FindPlayerTarget(target);
        if (player != null && player.IsValid && !player.IsFakeClient)
        {
            LogDebug($"Displaying stats menu for {player.Name} (UserID={player.UserID}, PlayerID={player.PlayerID}, Duration={duration}s)");
            DisplayDaemonStatsMenu(player, message, duration);
        }
        else if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            BroadcastChatMessage(message);
        }
        else
        {
            LogDebug($"hlx_sm_msay target '{target}' could not be matched to a connected human player.");
        }
    }

    private void ExecuteDaemonCommand(string rawCmd)
    {
        try
        {
            LogDebug($"Executing daemon command: {rawCmd}");

            if (rawCmd.StartsWith("say ", StringComparison.OrdinalIgnoreCase))
            {
                string text = rawCmd.Substring(4).Trim().Trim('"', '\'');
                BroadcastChatMessage(text);
                return;
            }
            if (rawCmd.StartsWith("hlx_sm_msay ", StringComparison.OrdinalIgnoreCase))
            {
                HandleMsayPayload(rawCmd.Substring(12).Trim());
                return;
            }
            if (rawCmd.StartsWith("hlx_sm_psay ", StringComparison.OrdinalIgnoreCase) || rawCmd.StartsWith("hlx_sm_psay2 ", StringComparison.OrdinalIgnoreCase))
            {
                int prefixLen = rawCmd.StartsWith("hlx_sm_psay2 ", StringComparison.OrdinalIgnoreCase) ? 13 : 12;
                string payload = rawCmd.Substring(prefixLen).Trim();
                if (payload.StartsWith('"') || payload.StartsWith('\''))
                {
                    int secondQuote = payload.IndexOf(payload[0], 1);
                    if (secondQuote > 0 && secondQuote < payload.Length - 1)
                    {
                        string possibleTarget = payload.Substring(1, secondQuote - 1).Trim();
                        string remainder = payload.Substring(secondQuote + 1).Trim();
                        if (possibleTarget.Equals("0") || possibleTarget.Equals("ALL", StringComparison.OrdinalIgnoreCase) || possibleTarget.Contains(',') || int.TryParse(possibleTarget, out _))
                        {
                            var remParts = remainder.Split(' ', 2);
                            string text = (remParts.Length >= 2 && int.TryParse(remParts[0], out _) ? remParts[1] : remainder).Trim('"', '\'');
                            if (possibleTarget.Equals("0") || possibleTarget.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                            {
                                BroadcastChatMessage(text);
                            }
                            else
                            {
                                foreach (var idStr in possibleTarget.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                {
                                    var player = FindPlayerTarget(idStr);
                                    if (player != null && player.IsValid && !player.IsFakeClient)
                                        SendChatMessage(player, text);
                                }
                            }
                            return;
                        }
                    }
                    string broadcastText = payload.Trim('"', '\'');
                    BroadcastChatMessage(broadcastText);
                    return;
                }

                var parts = payload.Split(' ', 3);
                if (parts.Length >= 2)
                {
                    string target = parts[0];
                    string text = (parts.Length >= 3 ? parts[2] : parts[1]).Trim('"', '\'');
                    if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        BroadcastChatMessage(text);
                    }
                    else
                    {
                        foreach (var idStr in target.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var player = FindPlayerTarget(idStr);
                            if (player != null && player.IsValid && !player.IsFakeClient)
                                SendChatMessage(player, text);
                        }
                    }
                    return;
                }
                else if (parts.Length == 1)
                {
                    string text = parts[0].Trim('"', '\'');
                    BroadcastChatMessage(text);
                    return;
                }
            }
            if (rawCmd.StartsWith("hlx_sm_csay ", StringComparison.OrdinalIgnoreCase))
            {
                string text = rawCmd.Substring(12).Trim().Trim('"', '\'');
                ShowCenterHud(null, text, 4f);
                return;
            }
            if (rawCmd.StartsWith("hlx_sm_tsay ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawCmd.Substring(12).Trim().Split(' ', 3);
                if (parts.Length >= 2)
                {
                    string target = parts[0];
                    string text = (parts.Length >= 3 ? parts[2] : parts[1]).Trim('"', '\'');
                    if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowTopLeftHud(null, text, 4f);
                    }
                    else
                    {
                        var player = FindPlayerTarget(target);
                        if (player != null && player.IsValid && !player.IsFakeClient)
                            ShowTopLeftHud(player, text, 4f);
                    }
                    return;
                }
            }
            if (rawCmd.StartsWith("hlx_sm_hint ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawCmd.Substring(12).Trim().Split(' ', 2);
                if (parts.Length >= 2)
                {
                    string target = parts[0];
                    string text = parts[1].Trim('"', '\'');
                    if (target.Equals("0") || target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                            p.SendMessage(MessageType.Alert, text);
                    }
                    else
                    {
                        var player = FindPlayerTarget(target);
                        if (player != null && player.IsValid && !player.IsFakeClient)
                            player.SendMessage(MessageType.Alert, text);
                    }
                    return;
                }
            }
            if (rawCmd.StartsWith("hlxce_webpage ", StringComparison.OrdinalIgnoreCase))
            {
                _webpageUrl = rawCmd.Substring(14).Trim().Trim('"', '\'');
                return;
            }
            if (rawCmd.StartsWith("hlxce_version ", StringComparison.OrdinalIgnoreCase))
            {
                _hlxceVersion = rawCmd.Substring(14).Trim().Trim('"', '\'');
                return;
            }
            /* Fallback: execute as server command */
            Core.Engine.ExecuteCommand(rawCmd);
        }
        catch (Exception ex) { Console.WriteLine($"[HLstatsX:CE] ExecuteDaemonCommand error: {ex.Message}"); }
    }

    private void SendUdpLog(string logLine)
    {
        try
        {
            if (_udpClient == null)
            {
                _udpClient = new UdpClient();
            }
            var now = DateTime.Now;
            var formatted = $"L {now:MM/dd/yyyy} - {now:HH:mm:ss}: {logLine}\n";
            if (!string.IsNullOrEmpty(_config.ProxyKey) && !string.IsNullOrEmpty(_config.ServerIp))
            {
                formatted = $"PROXY Key={_config.ProxyKey} {_config.ServerIp}:{_config.ServerPort}PROXY {formatted}";
            }
            LogDebug($"UDP TX to {_config.DaemonHost}:{_config.DaemonPort}: {logLine}");
            var data = Encoding.UTF8.GetBytes(formatted);
            _udpClient.Send(data, data.Length, _config.DaemonHost, _config.DaemonPort);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] UDP send error: {ex.Message}");
            try { _udpClient?.Close(); _udpClient?.Dispose(); } catch { }
            _udpClient = null;
        }
    }

    private void AnnouncePlayerOnMap(IPlayer player)
    {
        if (player == null || !player.IsValid) return;
        SendPlayerConnectLogs(player);
        var team = GetTeamName(player);
        if (team != "Unassigned")
            SendLog(player, team, "joined team");
        SendPlayerClanTag(player, force: true);
        _mapAnnouncedPlayers.Add(player.PlayerID);
    }

    private void AnnounceAllConnectedPlayers()
    {
        foreach (var player in Core.PlayerManager.GetAllValidPlayers())
        {
            AnnouncePlayerOnMap(player);
        }
    }

    private void SendPlayerClanTag(IPlayer player, bool force = false)
    {
        if (player == null || !player.IsValid || player.IsFakeClient || player.SteamID == 0) return;
        try
        {
            var rawClan = player.Controller?.Clan ?? "";
            var clanTag = rawClan.Replace('"', '\'').Trim();
            if (string.IsNullOrEmpty(clanTag)) return;

            if (!force && _playerClanTags.TryGetValue(player.SteamID, out var lastTag) && string.Equals(lastTag, clanTag, StringComparison.Ordinal))
                return;

            _playerClanTags[player.SteamID] = clanTag;
            SendUdpLog($"{FormatPlayerString(player)} triggered \"clantag\" (value \"{clanTag}\")");
        }
        catch (Exception ex)
        {
            LogDebug($"SendPlayerClanTag error: {ex.Message}");
        }
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
        SendPlayerClanTag(player, force: true);
        _mapAnnouncedPlayers.Add(player.PlayerID);
    }

    private void SendServerCvars()
    {
        SendUdpLog($"server_cvar: \"maxplayers\" \"{_config.MaxPlayers}\"");
        SendUdpLog($"server_cvar: \"sv_visiblemaxplayers\" \"{_config.MaxPlayers}\"");
        try
        {
            var conVar = Core.ConVar.Find<string>("hostname");
            if (conVar != null && !string.IsNullOrWhiteSpace(conVar.Value))
                SendUdpLog($"server_cvar: \"hostname\" \"{conVar.Value}\"");
        }
        catch { }
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
}

