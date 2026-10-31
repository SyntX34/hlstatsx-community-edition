using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
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

    private static readonly (int Kills, string Name)[] StreakMilestones = new[]
    {
        (2, "Double Kill"),
        (3, "Triple Kill"),
        (4, "Domination"),
        (5, "Rampage"),
        (6, "Mega Kill"),
        (7, "Ownage"),
        (8, "Ultra Kill"),
        (9, "Killing Spree"),
        (10, "Monster Kill"),
        (11, "Unstoppable"),
        (12, "God Like")
    };

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

    private static string FormatPlayerString(IPlayer player)
    {
        var name   = player.Name.Replace('"', '\'');
        var userid = player.UserID;
        var steam3 = GetSteamId3(player.SteamID);
        var team   = GetTeamName(player);
        return $"\"{name}<{userid}><{steam3}><{team}>\"";
    }

    private IPlayer? FindPlayerTarget(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        token = token.Trim().Trim('"', '\'', '#');

        if (int.TryParse(token, out int uid))
        {
            /* hlstats daemon passes engine userid (e.g. 7 from "Name<7><...>") */
            var matchUserId = Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(x => x.UserID == uid);
            if (matchUserId != null) return matchUserId;

            var p = Core.PlayerManager.GetPlayer(uid);
            if (p != null && p.IsValid) return p;
            return Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(x => x.Slot == uid);
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

    private void SendChatMessage(IPlayer player, string message)
    {
        if (player == null || !player.IsValid || player.IsFakeClient) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        var lines = message.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var formatted = FormatSourceModMessage(player, trimmed);
            if (!string.IsNullOrWhiteSpace(formatted))
                player.SendMessage(MessageType.Chat, formatted);
        }
    }

    private void BroadcastChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
            SendChatMessage(p, message);
    }

    private void RegisterGameEvents()
    {
        if (_chatHookGuid != Guid.Empty)
        {
            try { Core.Command.UnhookClientChat(_chatHookGuid); } catch { }
            _chatHookGuid = Guid.Empty;
        }

        _chatHookGuid = Core.Command.HookClientChat((playerId, text, teamOnly) =>
        {
            if (string.IsNullOrWhiteSpace(text)) return HookResult.Continue;
            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player == null || !player.IsValid) return HookResult.Continue;

            var trimmedText = text.Trim();

            string cleaned = trimmedText;
            bool hasPrefix = false;
            if (cleaned.StartsWith('!') || cleaned.StartsWith('/'))
            {
                hasPrefix = true;
                cleaned = cleaned.Substring(1).Trim();
            }

            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                string cmd = parts[0];
                if (MenuCommands.Contains(cmd))
                {
                    OpenStatsMenu(player);
                    return HookResult.Handled;
                }

                if (IsStatCommand(cmd, cleaned))
                {
                    string verb = teamOnly ? "say_team" : "say";
                    DispatchPlayerStatCommand(player, cleaned, verb);
                    if (_blockChatCommands || hasPrefix) return HookResult.Handled;
                    return HookResult.Continue;
                }
            }

            /* Ignore non-HLstatsX slash commands so they don't get logged to public chat */
            if (trimmedText.StartsWith('/')) return HookResult.Continue;

            string chatVerb = teamOnly ? "say_team" : "say";
            SendLog(player, trimmedText, chatVerb);
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
            _playerStreak.Clear();
            AnnounceCurrentMap();
            SendUdpLog("World triggered \"Round_Start\"");
            foreach (var player in Core.PlayerManager.GetAllValidPlayers())
            {
                if (!_mapAnnouncedPlayers.Contains(player.PlayerID))
                    AnnouncePlayerOnMap(player);
            }
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
            {
                if (!_mapAnnouncedPlayers.Contains(player.PlayerID))
                    AnnouncePlayerOnMap(player);
                SendPlayerClanTag(player);
                SendLog(player, "spawned", "triggered");
            }
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
            if (team == "TERRORIST" || team == "CT")
                _mapAnnouncedPlayers.Add(player.PlayerID);
            else
                _mapAnnouncedPlayers.Remove(player.PlayerID);
            SendPlayerClanTag(player);
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerChangename>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            var newName = @event.NewName?.Replace('"', '\'') ?? "";
            if (!string.IsNullOrEmpty(newName))
                SendLog(player, newName, "changed name to");
            SendPlayerClanTag(player);
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
            Core.Scheduler.DelayBySeconds(2.0f, () =>
            {
                if (player != null && player.IsValid)
                    SendPlayerClanTag(player, force: true);
            });
            return HookResult.Continue;
        });

        Core.GameEvent.HookPost<EventPlayerDisconnect>((@event) =>
        {
            var player = @event.UserIdPlayer;
            if (player == null || !player.IsValid) return HookResult.Continue;
            if (player.SteamID > 0)
            {
                _playerStreak.Remove(player.SteamID);
                _playerClanTags.Remove(player.SteamID);
            }
            _mapConnectedPlayers.Remove(player.PlayerID);
            _mapAnnouncedPlayers.Remove(player.PlayerID);
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
            DisposePlayerMenu(player.PlayerID);
            CloseCustomHud(player.PlayerID);
            CloseCsayHud(player.PlayerID);
            CloseTsayHud(player.PlayerID);
            return HookResult.Continue;
        });

        Core.Event.OnMapLoad -= HandleMapLoad;
        Core.Event.OnMapLoad += HandleMapLoad;

        Core.Event.OnMapUnload -= HandleMapUnload;
        Core.Event.OnMapUnload += HandleMapUnload;

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
                /* Reset streak for victim */
                if (victim.SteamID > 0)
                    _playerStreak[victim.SteamID] = 0;

                if (attacker != null && attacker.IsValid && attacker.UserID != victim.UserID)
                {
                    string headshotProp = @event.Headshot ? " (headshot)" : "";
                    SendUdpLog($"{FormatPlayerString(attacker)} killed {FormatPlayerString(victim)} with \"{finalWeapon}\"{headshotProp}");

                    /* Track attacker streak */
                    if (attacker.SteamID > 0)
                    {
                        _playerStreak.TryGetValue(attacker.SteamID, out int curStreak);
                        curStreak++;
                        _playerStreak[attacker.SteamID] = curStreak;

                        /* Check milestones */
                        var matchedMilestone = StreakMilestones.FirstOrDefault(m => m.Kills == curStreak);
                        if (matchedMilestone.Kills > 0)
                        {
                            SendUdpLog($"{FormatPlayerString(attacker)} triggered \"kill_streak_{curStreak}\"");
                            if (curStreak >= 3)
                            {
                                string streakName = matchedMilestone.Name;
                                int kills = curStreak;
                                string attackerName = attacker.Name;
                                foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                                {
                                    var loc = Core.Translation.GetPlayerLocalizer(p);
                                    string tmpl = loc["hlx.streak"] ?? "[red][HLstatsX][default] [green]{0}[default] is on a [green]{1}[default] streak ({2} kills)!";
                                    p.SendMessage(MessageType.Chat, FormatColors(string.Format(tmpl, attackerName, streakName, kills)));
                                }
                            }
                        }
                        else if (curStreak > 12)
                        {
                            SendUdpLog($"{FormatPlayerString(attacker)} triggered \"kill_streak_12\"");
                            int kills = curStreak;
                            string attackerName = attacker.Name;
                            foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                            {
                                var loc = Core.Translation.GetPlayerLocalizer(p);
                                string tmpl = loc["hlx.streak"] ?? "[red][HLstatsX][default] [green]{0}[default] is on a [green]{1}[default] streak ({2} kills)!";
                                p.SendMessage(MessageType.Chat, FormatColors(string.Format(tmpl, attackerName, "God Like", kills)));
                            }
                        }
                    }
                }
                else if (attacker == null || attacker.UserID == victim.UserID)
                {
                    SendLog(victim, finalWeapon, "committed suicide with");
                }

                if (@event.AssisterPlayer != null && @event.AssisterPlayer.IsValid && !@event.AssisterPlayer.IsFakeClient)
                    SendUdpLog($"{FormatPlayerString(@event.AssisterPlayer)} triggered \"assisted_kill\" against {FormatPlayerString(victim)}");

                if (@event.Dominated > 0 && attacker != null && attacker.IsValid)
                {
                    SendUdpLog($"{FormatPlayerString(attacker)} triggered \"domination\" against {FormatPlayerString(victim)}");
                    string attackerName = attacker.Name;
                    string victimName = victim.Name;
                    foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                    {
                        var loc = Core.Translation.GetPlayerLocalizer(p);
                        string tmpl = loc["hlx.domination"] ?? "[red][HLstatsX][default] [green]{0}[default] is [green]DOMINATING[default] [green]{1}[default]!";
                        p.SendMessage(MessageType.Chat, FormatColors(string.Format(tmpl, attackerName, victimName)));
                    }
                }

                if (@event.Revenge > 0 && attacker != null && attacker.IsValid)
                {
                    SendUdpLog($"{FormatPlayerString(attacker)} triggered \"revenge\" against {FormatPlayerString(victim)}");
                    string attackerName = attacker.Name;
                    string victimName = victim.Name;
                    foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(x => !x.IsFakeClient))
                    {
                        var loc = Core.Translation.GetPlayerLocalizer(p);
                        string tmpl = loc["hlx.revenge"] ?? "[red][HLstatsX][default] [green]{0}[default] got [green]REVENGE[default] on [green]{1}[default]!";
                        p.SendMessage(MessageType.Chat, FormatColors(string.Format(tmpl, attackerName, victimName)));
                    }
                }
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

        Core.Event.OnCustomHudClicked -= HandleCustomHudClick;
        Core.Event.OnCustomHudClicked += HandleCustomHudClick;
    }

    private void UnregisterGameEvents()
    {
        if (_chatHookGuid != Guid.Empty)
        {
            try { Core.Command.UnhookClientChat(_chatHookGuid); } catch { }
            _chatHookGuid = Guid.Empty;
        }
        Core.Event.OnMapLoad -= HandleMapLoad;
        Core.Event.OnMapUnload -= HandleMapUnload;
        Core.Event.OnCustomHudClicked -= HandleCustomHudClick;
    }

    private void HandleMapUnload(IOnMapUnloadEvent @event)
    {
        try
        {
            foreach (var player in Core.PlayerManager.GetAllValidPlayers())
                FlushPlayerWeaponStats(player);
        }
        catch { }

        ClearAllMapState();
    }

    private void DispatchPlayerStatCommand(IPlayer player, string commandText, string verb = "say")
    {
        if (player == null || !player.IsValid || string.IsNullOrWhiteSpace(commandText)) return;
        int playerId = player.PlayerID;
        long now = Environment.TickCount64;
        string normalized = commandText.Trim();

        if (_lastPlayerCommand.TryGetValue(playerId, out var prev) &&
            prev.Command.Equals(normalized, StringComparison.OrdinalIgnoreCase) &&
            now - prev.Tick >= 0 && now - prev.Tick < 250)
        {
            return;
        }

        _lastPlayerCommand[playerId] = (normalized, now);
        SendLog(player, normalized, verb);
    }

    private string ResolveCurrentMapName(string? rawMap = null)
    {
        string candidate = rawMap ?? "";
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Equals("<empty>", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                candidate = Core.Engine.GlobalVars.MapName.ToString() ?? "";
            }
            catch
            {
                candidate = "";
            }
        }

        candidate = candidate.Trim().Replace('\\', '/');
        if (candidate.Equals("<empty>", StringComparison.OrdinalIgnoreCase))
            candidate = "";

        if (candidate.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
            candidate = candidate.Substring(0, candidate.Length - 4);

        if (candidate.Contains('/'))
            candidate = candidate.Substring(candidate.LastIndexOf('/') + 1);

        if (string.IsNullOrWhiteSpace(candidate))
        {
            try
            {
                string workshopId = Core.Engine.WorkshopId?.Trim() ?? "";
                if (!string.IsNullOrEmpty(workshopId))
                    candidate = $"workshop_{workshopId}";
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(candidate))
            _currentMapName = candidate;

        return _currentMapName;
    }

    private void AnnounceCurrentMap(string? rawMap = null, bool logLoading = false)
    {
        string mapName = ResolveCurrentMapName(rawMap);
        if (string.IsNullOrWhiteSpace(mapName)) return;

        if (logLoading)
            SendUdpLog($"Loading map \"{mapName}\"");
        SendUdpLog($"Started map \"{mapName}\" (CRC \"-1\")");
    }

    private void HandleMapLoad(IOnMapLoadEvent @event)
    {
        ClearAllMapState();
        RestartUdpConnection();

        SendServerCvars();
        AnnounceCurrentMap(@event.MapName, logLoading: true);

        /* Re-announce players who remained connected across the map transition */
        AnnounceAllConnectedPlayers();
        Core.Scheduler.DelayBySeconds(2.0f, () =>
        {
            AnnounceCurrentMap();
            AnnounceAllConnectedPlayers();
        });
        Core.Scheduler.DelayBySeconds(5.0f, () =>
        {
            AnnounceCurrentMap();
            AnnounceAllConnectedPlayers();
        });
    }

    private void DisposePlayerMenu(int playerId)
    {
        if (_playerMenus.TryGetValue(playerId, out var menu))
        {
            try { menu?.Dispose(); } catch { }
            _playerMenus.Remove(playerId);
        }
    }

    private void ClearAllMapState()
    {
        _Statsme.Clear();
        _Statsme2.Clear();
        _playerStreak.Clear();
        _playerClanTags.Clear();
        _mapConnectedPlayers.Clear();
        _mapAnnouncedPlayers.Clear();
        _customHudStatsState.Clear();
        _lastPlayerCommand.Clear();

        foreach (var kvp in _playerMenus)
        {
            try { kvp.Value?.Dispose(); } catch { }
        }
        _playerMenus.Clear();

        if (_builtinMainMenu != null)
        {
            try { _builtinMainMenu.Dispose(); } catch { }
            _builtinMainMenu = null;
        }

        CloseAllCustomHuds();
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
        @"(?<killer>.+?)\s*\((?<kpts>[\d,]+)\)(?<kextra>.*?)\s*got\s*(?<pts>[+-]?\d+)\s*points(?<vextra>.*?)\s*for killing\s*(?<victim>.+?)\s*\((?<vpts>[\d,]+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex KillRewardSimpleRegex = new(
        @"(?<killer>.+?)\s*\((?<kpts>[\d,]+)\)(?<kextra>.*?)\s*got\s*(?<pts>[+-]?\d+)\s*points\s*for killing\s*(?<victim>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PlayerPlayerActionRegex = new(
        @"(?<player>.+?)\s+(?<verb>got|lost)\s+(?<pts>[\d,]+)\s+points\s*\((?<ppts>[\d,]+)\)\s*for\s+(?<action>.+?)\s+against\s+(?<victim>.+?)\s*\((?<vpts>[\d,]+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PlayerActionRegex = new(
        @"(?<player>.+?)\s+(?<verb>got|lost)\s+(?<pts>[\d,]+)\s+points\s*\((?<ppts>[\d,]+)\)\s*for\s+(?<action>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ActionRewardRegex = new(
        @"(?<team>.+?)\s+(?<verb>got|lost)\s+(?<pts>[\d,]+)\s+points\s+for\s+(?<action>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TeamkillPenaltyRegex = new(
        @"(?<killer>.+?)\s*lost\s*(?<pts>[\d,]+)\s*points\s*\((?<total>[\d,]+)\)\s*for team-killing",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RankMsgRegex = new(
        @"(?<player>.+?)\s*is on rank\s*#?(?<rank>\d+)\s*of\s*(?<total>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)!?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RankHiddenMsgRegex = new(
        @"(?<player>.+?)\s*is on rank\s*\(HIDDEN\)\s*of\s*(?<total>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)!?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectRankCountryRegex = new(
        @"(?<player>.+?)\s*\(Pos\s*(?<rank>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)\)\s*has connected from\s*(?<country>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectRankRegex = new(
        @"(?<player>.+?)\s*\(Pos\s*(?<rank>\d+)\s*with\s*(?<pts>[\d,]+)\s*(?:points|kills)\)\s*has connected",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NewConnectCountryRegex = new(
        @"New player\s+(?<player>.+?)\s+has connected from\s+(?<country>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectCountryRegex = new(
        @"Player\s+(?<player>.+?)\s+has connected from\s+(?<country>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex KDeathMsgRegex = new(
        @"(?<player>.+?)\s+has\s+(?<kills>\d+):(?<deaths>\d+)\s+frags,\s+(?<hs>\d+)\s+headshots\s*\((?<hpk>[\d.]+)%\),(?:\s*(?<acc>[\d.]+)%?\s*accuracy,)?\s*and a KD-Ratio of\s*(?<kd>[\d.]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SessionMsgRegex = new(
        @"(?<player>.+?)\s+has\s+(?<kills>\d+):(?<deaths>\d+)\s+frags(?:\s*\((?<ratio>[\d.]+)%\))?,\s+(?<hs>\d+)\s+headshots\s*\((?<hpk>[\d.]+)%\),(?:\s*(?<acc>[\d.]+)%?\s*accuracy,)?(?:\s*and a skill change of\s*(?<pts>[+-]?\d+)\s*points)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NextMsgRegex = new(
        @"Next ranked (?:player )?above you:\s*(?<player>.+?)\s*\(Rank\s*#?(?<rank>\d+)\s*with\s*(?<pts>[\d,]+)\s*points\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string CleanControlCodes(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        var sb = new StringBuilder(str.Length);
        foreach (char c in str)
        {
            if (c >= 1 && c <= 16) continue;
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private string FormatSourceModMessage(IPlayer player, string rawMsg)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawMsg)) return "";
            var loc = Core.Translation.GetPlayerLocalizer(player);

            string clean = CleanControlCodes(rawMsg);
            if (clean.StartsWith("[") && clean.Contains("]"))
            {
                int closeIdx = clean.IndexOf(']');
                if (closeIdx > 0 && closeIdx < clean.Length - 1)
                {
                    string candidate = clean.Substring(closeIdx + 1).Trim();
                    if (!string.IsNullOrEmpty(candidate))
                        clean = candidate;
                }
            }

            var m = ConnectRankCountryRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.player_connected_rank_country"] ?? "[red][HLstatsX][default] [yellow]{0}[default] (Pos [lightred]#{1}[default] with [lime]{2}[default] points) has connected from [yellow]{3}[default]!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["rank"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["country"].Value.Trim());
                return FormatColors(formatted);
            }

            m = ConnectRankRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.player_connected_rank"] ?? "[red][HLstatsX][default] [yellow]{0}[default] (Pos [lightred]#{1}[default] with [lime]{2}[default] points) has connected!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["rank"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = NewConnectCountryRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.new_player_connected_country"] ?? "[red][HLstatsX][default] New player [yellow]{0}[default] has connected from [yellow]{1}[default]!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["country"].Value.Trim());
                return FormatColors(formatted);
            }

            m = ConnectCountryRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.player_connected_country"] ?? "[red][HLstatsX][default] Player [yellow]{0}[default] has connected from [yellow]{1}[default]!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["country"].Value.Trim());
                return FormatColors(formatted);
            }

            m = KillRewardRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.kill_reward"] ?? "[red][HLstatsX][default] [green]{0}[default] ({1}) got [lime]+{2} points[default] for killing [green]{3}[default] ({4})!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["kpts"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["victim"].Value.Trim(), m.Groups["vpts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = KillRewardSimpleRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.kill_reward"] ?? "[red][HLstatsX][default] [green]{0}[default] ({1}) got [lime]+{2} points[default] for killing [green]{3}[default] ({4})!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["kpts"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["victim"].Value.Trim(), "-");
                return FormatColors(formatted);
            }

            m = TeamkillPenaltyRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.teamkill_penalty"] ?? "[red][HLstatsX][default] [green]{0}[default] lost [red]-{1} points[default] ({2}) for team-killing!";
                string formatted = string.Format(tmpl, m.Groups["killer"].Value.Trim(), m.Groups["pts"].Value.Trim(), m.Groups["total"].Value.Trim());
                return FormatColors(formatted);
            }

            m = PlayerPlayerActionRegex.Match(clean);
            if (m.Success)
            {
                string verb = m.Groups["verb"].Value.ToLowerInvariant();
                string tmpl = loc["hlx.player_player_action"] ?? "[red][HLstatsX][default] [green]{0}[default] {1} [lime]{2} points[default] ({3}) for [green]{4}[default] against [green]{5}[default] ({6})!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), verb, m.Groups["pts"].Value.Trim(), m.Groups["ppts"].Value.Trim(), m.Groups["action"].Value.Trim(), m.Groups["victim"].Value.Trim(), m.Groups["vpts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = PlayerActionRegex.Match(clean);
            if (m.Success)
            {
                string verb = m.Groups["verb"].Value.ToLowerInvariant();
                string tmpl = loc["hlx.player_action"] ?? "[red][HLstatsX][default] [green]{0}[default] {1} [lime]{2} points[default] ({3}) for [green]{4}[default]!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), verb, m.Groups["pts"].Value.Trim(), m.Groups["ppts"].Value.Trim(), m.Groups["action"].Value.Trim());
                return FormatColors(formatted);
            }

            m = ActionRewardRegex.Match(clean);
            if (m.Success)
            {
                string verb = m.Groups["verb"].Value.ToLowerInvariant();
                string key = verb == "lost" ? "hlx.points_lost" : "hlx.points_got";
                string tmpl = loc[key] ?? (verb == "lost"
                    ? "[red][HLstatsX][default] You [red]lost -{0} points[default] ({1}) for [green]{2}[default]!"
                    : "[red][HLstatsX][default] You [lime]got +{0} points[default] ({1}) for [green]{2}[default]!");
                string formatted = string.Format(tmpl, m.Groups["pts"].Value.Trim(), m.Groups["team"].Value.Trim(), m.Groups["action"].Value.Trim());
                return FormatColors(formatted);
            }

            m = RankMsgRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.rank"] ?? "[red][HLstatsX][default] [yellow]{0}[default] is on rank [lightred]#{1}[default] of [lightred]{2}[default] with [lightred]{3}[default] points!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["rank"].Value.Trim(), m.Groups["total"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = RankHiddenMsgRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.rank_hidden"] ?? "[red][HLstatsX][default] [yellow]{0}[default] is on rank [grey](HIDDEN)[default] of [lightred]{1}[default] with [lightred]{2}[default] points!";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["total"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }

            m = KDeathMsgRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.kdeath"] ?? "[red][HLstatsX][default] [yellow]{0}[default] stats: [lime]{1} kills[default], [red]{2} deaths[default] (K/D: [yellow]{3}[default]), [lime]{4} HS[default] ({5}%), Acc: [yellow]{6}[default]%";
                string acc = string.IsNullOrWhiteSpace(m.Groups["acc"].Value) ? "0.0" : m.Groups["acc"].Value.Trim();
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["kills"].Value.Trim(), m.Groups["deaths"].Value.Trim(), m.Groups["kd"].Value.Trim(), m.Groups["hs"].Value.Trim(), m.Groups["hpk"].Value.Trim(), acc);
                return FormatColors(formatted);
            }

            m = SessionMsgRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.session"] ?? "[red][HLstatsX][default] [yellow]{0}[default] session: [lime]{1} kills[default], [red]{2} deaths[default] (K/D: [yellow]{3}[default]), [lime]{4} HS[default] ({5}%), Acc: [yellow]{6}[default]%";
                string kills = m.Groups["kills"].Value.Trim();
                string deaths = m.Groups["deaths"].Value.Trim();
                string kd = double.TryParse(kills, out double k) && double.TryParse(deaths, out double d) && d > 0 ? (k / d).ToString("F2") : kills;
                string acc = string.IsNullOrWhiteSpace(m.Groups["acc"].Value) ? "0.0" : m.Groups["acc"].Value.Trim();
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), kills, deaths, kd, m.Groups["hs"].Value.Trim(), m.Groups["hpk"].Value.Trim(), acc);
                return FormatColors(formatted);
            }

            m = NextMsgRegex.Match(clean);
            if (m.Success)
            {
                string tmpl = loc["hlx.next"] ?? "[red][HLstatsX][default] Next ranked above you: [yellow]{0}[default] (Rank #{1} with {2} points)";
                string formatted = string.Format(tmpl, m.Groups["player"].Value.Trim(), m.Groups["rank"].Value.Trim(), m.Groups["pts"].Value.Trim());
                return FormatColors(formatted);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] FormatSourceModMessage error: {ex.Message}");
        }

        string prefix = string.IsNullOrEmpty(_messagePrefix) ? "[red][HLstatsX][default] " : _messagePrefix;
        string cleanMsg = rawMsg.Trim();
        if (cleanMsg.StartsWith("[HLstatsX]", StringComparison.OrdinalIgnoreCase))
            cleanMsg = cleanMsg.Substring(10).Trim();
        else if (cleanMsg.StartsWith("[red][HLstatsX][default]", StringComparison.OrdinalIgnoreCase))
            cleanMsg = cleanMsg.Substring(24).Trim();
        else if (cleanMsg.StartsWith("[green][HLstatsX][default]", StringComparison.OrdinalIgnoreCase))
            cleanMsg = cleanMsg.Substring(26).Trim();
        else if (cleanMsg.StartsWith("\x07[HLstatsX]\x01", StringComparison.OrdinalIgnoreCase) || cleanMsg.StartsWith("\x04[HLstatsX]\x01", StringComparison.OrdinalIgnoreCase))
            cleanMsg = cleanMsg.Substring(12).Trim();

        return FormatColors($"{prefix}{cleanMsg}");
    }

    private void CancelMenuAutoClose(int playerId)
    {
        if (_menuCloseTimers.TryGetValue(playerId, out var cts))
        {
            try { cts?.Cancel(); } catch { }
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
                try { Core.Menu.CloseFor(player); } catch { }
                DisposePlayerMenu(playerId);
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
                try { kvp.Value?.Cancel(); } catch { }
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

            foreach (var kvp in _csayTimers)
            {
                try { kvp.Value?.Cancel(); } catch { }
            }
            _csayTimers.Clear();

            foreach (var kvp in _csayHuds)
            {
                var hud = kvp.Value;
                if (hud != null && hud.IsValid)
                {
                    SetHudClass(hud, "HlxCsayPanel", "Visible", false);
                    hud.Despawn();
                }
            }
            _csayHuds.Clear();

            foreach (var kvp in _tsayTimers)
            {
                try { kvp.Value?.Cancel(); } catch { }
            }
            _tsayTimers.Clear();

            foreach (var kvp in _tsayHuds)
            {
                var hud = kvp.Value;
                if (hud != null && hud.IsValid)
                {
                    SetHudClass(hud, "HlxTsayPanel", "Visible", false);
                    hud.Despawn();
                }
            }
            _tsayHuds.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CloseAllCustomHuds error: {ex.Message}");
        }
    }

    private void CloseCsayHud(int playerId)
    {
        try
        {
            if (_csayTimers.TryGetValue(playerId, out var cts))
            {
                try { cts?.Cancel(); } catch { }
                _csayTimers.Remove(playerId);
            }

            if (_csayHuds.TryGetValue(playerId, out var hud))
            {
                if (hud != null && hud.IsValid)
                {
                    SetHudClass(hud, "HlxCsayPanel", "Visible", false);
                    hud.Despawn();
                }
                _csayHuds.Remove(playerId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CloseCsayHud error: {ex.Message}");
        }
    }

    private void CloseTsayHud(int playerId)
    {
        try
        {
            if (_tsayTimers.TryGetValue(playerId, out var cts))
            {
                try { cts?.Cancel(); } catch { }
                _tsayTimers.Remove(playerId);
            }

            if (_tsayHuds.TryGetValue(playerId, out var hud))
            {
                if (hud != null && hud.IsValid)
                {
                    SetHudClass(hud, "HlxTsayPanel", "Visible", false);
                    hud.Despawn();
                }
                _tsayHuds.Remove(playerId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] CloseTsayHud error: {ex.Message}");
        }
    }

    private void ShowCenterHud(IPlayer? target, string message, float duration = 4f)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        bool isCustomHud = _config.MenuType.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                           _config.MenuType.Equals("CustomHud", StringComparison.OrdinalIgnoreCase);

        if (!isCustomHud)
        {
            if (target != null)
            {
                if (target.IsValid && !target.IsFakeClient)
                    target.SendMessage(MessageType.Center, message);
            }
            else
            {
                foreach (var p in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    p.SendMessage(MessageType.Center, message);
            }
            return;
        }

        IEnumerable<IPlayer> recipients = target != null
            ? (target.IsValid && !target.IsFakeClient ? new[] { target } : Array.Empty<IPlayer>())
            : Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient);

        foreach (var player in recipients)
        {
            try
            {
                int playerId = player.PlayerID;
                if (_csayTimers.TryGetValue(playerId, out var oldCts))
                {
                    try { oldCts?.Cancel(); } catch { }
                    _csayTimers.Remove(playerId);
                }

                CCSCustomHudLayout hud;
                if (_csayHuds.TryGetValue(playerId, out var existingHud) && existingHud != null && existingHud.IsValid)
                {
                    hud = existingHud;
                    hud.SetTransmitState(true, playerId);
                }
                else
                {
                    hud = Core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
                    hud.StrLayout = _config.CustomCsayLayout;
                    hud.StrLayoutUpdated();
                    hud.DispatchSpawn();

                    hud.SetTransmitState(false);
                    hud.SetTransmitState(true, playerId);
                    _csayHuds[playerId] = hud;
                }

                /* Set dialog variable on both child label and parent panel so Panorama binding resolves cleanly */
                hud.SetDialogVariableString("HlxCsayText", "csay_text", message);
                hud.SetDialogVariableString("HlxCsayPanel", "csay_text", message);
                SetHudClass(hud, "HlxCsayPanel", "Visible", true);

                if (duration > 0)
                {
                    var cts = Core.Scheduler.DelayBySeconds(duration, () =>
                    {
                        CloseCsayHud(playerId);
                    });
                    _csayTimers[playerId] = cts;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HLstatsX:CE] ShowCenterHud error: {ex.Message}");
                try { player.SendMessage(MessageType.Center, message); } catch { }
            }
        }
    }

    private void ShowTopLeftHud(IPlayer? target, string message, float duration = 4f)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        bool isCustomHud = _config.MenuType.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                           _config.MenuType.Equals("CustomHud", StringComparison.OrdinalIgnoreCase);

        if (!isCustomHud)
        {
            if (target != null)
            {
                if (target.IsValid && !target.IsFakeClient)
                    SendChatMessage(target, message);
            }
            else
            {
                BroadcastChatMessage(message);
            }
            return;
        }

        IEnumerable<IPlayer> recipients = target != null
            ? (target.IsValid && !target.IsFakeClient ? new[] { target } : Array.Empty<IPlayer>())
            : Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient);

        foreach (var player in recipients)
        {
            try
            {
                int playerId = player.PlayerID;
                if (_tsayTimers.TryGetValue(playerId, out var oldCts))
                {
                    try { oldCts?.Cancel(); } catch { }
                    _tsayTimers.Remove(playerId);
                }

                CCSCustomHudLayout hud;
                if (_tsayHuds.TryGetValue(playerId, out var existingHud) && existingHud != null && existingHud.IsValid)
                {
                    hud = existingHud;
                    hud.SetTransmitState(true, playerId);
                }
                else
                {
                    hud = Core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
                    hud.StrLayout = _config.CustomTsayLayout;
                    hud.StrLayoutUpdated();
                    hud.DispatchSpawn();

                    hud.SetTransmitState(false);
                    hud.SetTransmitState(true, playerId);
                    _tsayHuds[playerId] = hud;
                }

                /* Set dialog variable on both child label and parent panel so Panorama binding resolves cleanly */
                hud.SetDialogVariableString("HlxTsayText", "tsay_text", message);
                hud.SetDialogVariableString("HlxTsayPanel", "tsay_text", message);
                SetHudClass(hud, "HlxTsayPanel", "Visible", true);

                if (duration > 0)
                {
                    var cts = Core.Scheduler.DelayBySeconds(duration, () =>
                    {
                        CloseTsayHud(playerId);
                    });
                    _tsayTimers[playerId] = cts;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HLstatsX:CE] ShowTopLeftHud error: {ex.Message}");
                try { SendChatMessage(player, message); } catch { }
            }
        }
    }

    private void CloseCustomHud(int playerId)
    {
        try
        {
            CancelMenuAutoClose(playerId);
            _activeHudPlayers.Remove(playerId);
            _customHudStatsState.Remove(playerId);
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

        bool isCustomHud = _config.MenuType.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                           _config.MenuType.Equals("CustomHud", StringComparison.OrdinalIgnoreCase);

        if (isCustomHud)
        {
            OpenCustomHudMenu(player);
            return;
        }

        OpenBuiltinMenu(player);
    }

    private CCSCustomHudLayout EnsureCustomHud(int playerId)
    {
        if (_playerHuds.TryGetValue(playerId, out var existing) && existing != null && existing.IsValid)
        {
            existing.SetTransmitState(true, playerId);
            return existing;
        }

        var hud = Core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        hud.StrLayout = _config.CustomMenuLayout;
        hud.StrLayoutUpdated();
        hud.DispatchSpawn();
        hud.SetTransmitState(false);
        hud.SetTransmitState(true, playerId);
        _playerHuds[playerId] = hud;
        return hud;
    }

    private void OpenCustomHudMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            var hud = EnsureCustomHud(playerId);

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

            for (int i = 1; i <= 7; i++)
            {
                string key = $"HlxMenuOption{i:D2}";
                SetHudClass(hud, key, "Hidden", false);
                SetHudClass(hud, key, "Interactive", true);
            }
            for (int i = 8; i <= 10; i++)
            {
                string key = $"HlxMenuOption{i:D2}";
                SetHudClass(hud, key, "Hidden", true);
                SetHudClass(hud, key, "Interactive", false);
            }

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

            /* Prefer HUD dict match; _activeHudPlayers is advisory only */
            CCSCustomHudLayout? hud = null;
            if (!_playerHuds.TryGetValue(playerId, out hud) || hud == null || !hud.IsValid)
            {
                /* Fallback: match by CustomHudLayout entity */
                var evtHud = @event.CustomHudLayout;
                if (evtHud != null)
                {
                    var found = _playerHuds.FirstOrDefault(kv => kv.Value != null && kv.Value.IsValid && kv.Value == evtHud);
                    if (found.Value != null) { playerId = found.Key; hud = found.Value; }
                    else return;
                }
                else return;
            }

            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player == null || !player.IsValid) return;

            string button = @event.ButtonId ?? "";
            if (string.IsNullOrEmpty(button)) return;

            /* Stats display pagination mode */
            if (_customHudStatsState.TryGetValue(playerId, out var state))
            {
                int totalPages = Math.Max(1, (int)Math.Ceiling(state.Lines.Count / 10.0));
                if (button == "HlxMenuBack")
                {
                    if (state.Page > 0)
                        Core.Scheduler.NextTick(() => RenderCustomHudStatsPage(player, hud, state.Title, state.Lines, state.Page - 1));
                    else
                    {
                        _customHudStatsState.Remove(playerId);
                        Core.Scheduler.NextTick(() => OpenCustomHudMenu(player));
                    }
                    return;
                }
                if (button == "HlxMenuNext")
                {
                    if (state.Page + 1 < totalPages)
                        Core.Scheduler.NextTick(() => RenderCustomHudStatsPage(player, hud, state.Title, state.Lines, state.Page + 1));
                    return;
                }
                if (button == "HlxMenuExit")
                {
                    _customHudStatsState.Remove(playerId);
                    Core.Scheduler.NextTick(() => CloseCustomHud(playerId));
                    return;
                }
                return;
            }

            /* Main menu: dispatch on NextTick (server thread requirement) */
            switch (button)
            {
                case "HlxMenuOption01":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "rank", "say"); });
                    break;
                case "HlxMenuOption02":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "top10", "say"); });
                    break;
                case "HlxMenuOption03":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "top20", "say"); });
                    break;
                case "HlxMenuOption04":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "next", "say"); });
                    break;
                case "HlxMenuOption05":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "session", "say"); });
                    break;
                case "HlxMenuOption06":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "statsme", "say"); });
                    break;
                case "HlxMenuOption07":
                    Core.Scheduler.NextTick(() => { CloseCustomHud(playerId); SendLog(player, "weapons", "say"); });
                    break;
                case "HlxMenuExit":
                    Core.Scheduler.NextTick(() => CloseCustomHud(playerId));
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] HandleCustomHudClick error: {ex.Message}");
        }
    }

    private static string EscapeCenterHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private IMenu GetOrCreateBuiltinMainMenu()
    {
        if (_builtinMainMenu != null) return _builtinMainMenu;

        var builder = Core.Menu.CreateMenu("hlstatsx.main")
            .WithTitle("► HLstatsX:CE Stats")
            .WithItemsPerPage(5);

        if (_config.AutoCloseMenuSeconds > 0)
            builder.WithAutoClose(TimeSpan.FromSeconds(_config.AutoCloseMenuSeconds));

        void AddCmdButton(string label, string cmd)
        {
            var btn = new ButtonComponent(label, ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() =>
                    {
                        CancelMenuAutoClose(p.PlayerID);
                        SendLog(p, cmd, "say");
                    });
                }
                return ValueTask.CompletedTask;
            })
            {
                CloseOnSelect = true
            };
            builder.Add(btn);
        }

        AddCmdButton("My Rank (!rank)",          "rank");
        AddCmdButton("Top 10 Players (!top10)",  "top10");
        AddCmdButton("Top 20 Players (!top20)",  "top20");
        AddCmdButton("Next Above Me (!next)",    "next");
        AddCmdButton("Session Stats (!session)", "session");
        AddCmdButton("My Stats (!statsme)",      "statsme");
        AddCmdButton("Weapon Usage (!weapons)",  "weapons");
        AddCmdButton("Weapon Accuracy (!accuracy)", "accuracy");
        AddCmdButton("Hitgroup Targets (!targets)", "targets");
        AddCmdButton("Top Victims (!kills)",     "kills");
        AddCmdButton("Action Summary (!actions)", "actions");
        AddCmdButton("Map Stats (!maps)",        "maps");
        AddCmdButton("Top Clans (!clans)",       "clans");
        AddCmdButton("Server List (!servers)",   "servers");
        AddCmdButton("Server Status (!status)",  "status");
        AddCmdButton("Toggle Ranking (!hlx_hideranking)", "hlx_hideranking");
        AddCmdButton("Help & Commands (!help)",  "help");

        _builtinMainMenu = builder.Build();
        return _builtinMainMenu;
    }

    private void OpenBuiltinMenu(IPlayer player)
    {
        try
        {
            DisposePlayerMenu(player.PlayerID);
            var menu = GetOrCreateBuiltinMainMenu();
            menu.Open(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Menu error: {ex.Message}");
            player.SendMessage(MessageType.Chat,
                FormatColors("[gold][HLstatsX:CE][default] Commands: [green]!rank  !top10  !session  !statsme  !next  !weapons"));
        }
    }

    private void DisplayDaemonStatsMenu(IPlayer player, string rawMessage, int duration = 15)
    {
        if (player == null || !player.IsValid) return;

        bool isCustomHud = _config.MenuType.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                           _config.MenuType.Equals("CustomHud", StringComparison.OrdinalIgnoreCase);

        if (isCustomHud)
        {
            DisplayCustomHudStats(player, rawMessage, duration);
            return;
        }

        DisplayBuiltinStatsMenu(player, rawMessage, duration);
    }

    private void DisplayBuiltinStatsMenu(IPlayer player, string rawMessage, int duration = 15)
    {
        try
        {
            int playerId = player.PlayerID;
            DisposePlayerMenu(playerId);

            var rawLines = rawMessage.Replace("\r\n", "\n").Split('\n');
            string title = "► HLstatsX:CE Stats";
            var optionLines = new List<string>();

            foreach (var rawLine in rawLines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("->", StringComparison.OrdinalIgnoreCase))
                {
                    var headerCandidate = line.Substring(2).Trim();
                    int dashIdx = headerCandidate.IndexOf(" - ", StringComparison.Ordinal);
                    if (dashIdx > 0 && dashIdx <= 3 && char.IsDigit(headerCandidate[0]))
                    {
                        headerCandidate = headerCandidate.Substring(dashIdx + 3).Trim();
                    }

                    if (optionLines.Count == 0 && title == "► HLstatsX:CE Stats")
                    {
                        title = $"► {EscapeCenterHtml(headerCandidate)}";
                    }
                    else
                    {
                        optionLines.Add($"── {EscapeCenterHtml(headerCandidate)} ──");
                    }
                    continue;
                }

                if (optionLines.Count == 0 && !char.IsDigit(line[0]) && title == "► HLstatsX:CE Stats")
                {
                    title = $"► {EscapeCenterHtml(line)}";
                }
                else
                {
                    optionLines.Add(EscapeCenterHtml(line));
                }
            }

            var builder = Core.Menu.CreateMenu($"hlstatsx.stats.{playerId}")
                .WithTitle(title)
                .WithItemsPerPage(5);

            if (duration > 0)
                builder.WithAutoClose(TimeSpan.FromSeconds(duration));

            if (optionLines.Count == 0)
            {
                builder.Add(new TextComponent(title) { Enabled = true });
            }
            else
            {
                foreach (var opt in optionLines)
                {
                    builder.Add(new TextComponent(opt) { Enabled = true });
                }
            }

            /* Navigation: Return to Main Menu */
            var backBtn = new ButtonComponent("« Main Menu", ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() =>
                    {
                        CancelMenuAutoClose(p.PlayerID);
                        OpenStatsMenu(p);
                    });
                }
                return ValueTask.CompletedTask;
            })
            {
                CloseOnSelect = true
            };
            builder.Add(backBtn);

            var menu = builder.Build();
            _playerMenus[playerId] = menu;
            menu.Open(player);

            if (duration > 0)
            {
                CancelMenuAutoClose(playerId);
                var cts = Core.Scheduler.DelayBySeconds(duration, () =>
                {
                    try { Core.Menu.CloseFor(player); } catch { }
                    DisposePlayerMenu(playerId);
                    _menuCloseTimers.Remove(playerId);
                });
                _menuCloseTimers[playerId] = cts;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] DisplayBuiltinStatsMenu error: {ex.Message}");
            SendChatMessage(player, rawMessage);
        }
    }

    private void RenderCustomHudStatsPage(IPlayer player, CCSCustomHudLayout hud, string title, List<string> lines, int page)
    {
        int playerId = player.PlayerID;
        int totalPages = Math.Max(1, (int)Math.Ceiling(lines.Count / 10.0));
        if (page < 0) page = 0;
        if (page >= totalPages) page = totalPages - 1;

        _customHudStatsState[playerId] = (title, lines, page);

        string pageTitle = totalPages > 1 ? $"{title} ({page + 1}/{totalPages})" : title;
        hud.SetDialogVariableString("HlxMenuTitle", "menu_title", pageTitle);

        int startIndex = page * 10;
        for (int i = 1; i <= 10; i++)
        {
            string key = $"HlxMenuOption{i:D2}";
            string labelKey = $"HlxMenuOption{i:D2}Label";
            string varName = $"option_{i:D2}";
            int itemIndex = startIndex + (i - 1);

            if (itemIndex < lines.Count)
            {
                hud.SetDialogVariableString(labelKey, varName, lines[itemIndex]);
                SetHudClass(hud, key, "Hidden", false);
                SetHudClass(hud, key, "Interactive", false);
            }
            else
            {
                hud.SetDialogVariableString(labelKey, varName, "");
                SetHudClass(hud, key, "Hidden", true);
                SetHudClass(hud, key, "Interactive", false);
            }
        }

        hud.SetDialogVariableString("HlxMenuBackLabel", "back_text", page > 0 ? "7. Prev" : "7. Menu");
        hud.SetDialogVariableString("HlxMenuNextLabel", "next_text", "8. Next");
        hud.SetDialogVariableString("HlxMenuExitLabel", "exit_text", "9. Exit");

        SetHudClass(hud, "HlxMenuBack", "Hidden", false);
        SetHudClass(hud, "HlxMenuBack", "Interactive", true);

        bool hasNext = page + 1 < totalPages;
        SetHudClass(hud, "HlxMenuNext", "Hidden", !hasNext);
        SetHudClass(hud, "HlxMenuNext", "Interactive", hasNext);

        SetHudClass(hud, "HlxMenuExit", "Hidden", false);
        SetHudClass(hud, "HlxMenuExit", "Interactive", true);

        hud.SetInputCaptureEnabledForPlayer(playerId, true);
        SetHudClass(hud, "HlxMenuPanel", "Visible", true);
        _activeHudPlayers.Add(playerId);
    }

    private void DisplayCustomHudStats(IPlayer player, string rawMessage, int duration = 15)
    {
        try
        {
            int playerId = player.PlayerID;
            var hud = EnsureCustomHud(playerId);

            var rawLines = rawMessage.Replace("\r\n", "\n").Split('\n');
            string title = "► HLstatsX:CE Stats";
            var optionLines = new List<string>();

            foreach (var rawLine in rawLines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("->", StringComparison.OrdinalIgnoreCase))
                {
                    line = line.Substring(2).Trim();
                }

                if (optionLines.Count == 0 && !char.IsDigit(line[0]))
                {
                    title = line;
                }
                else
                {
                    optionLines.Add(line);
                }
            }

            RenderCustomHudStatsPage(player, hud, title, optionLines, 0);

            CancelMenuAutoClose(playerId);
            if (duration > 0)
            {
                var cts = Core.Scheduler.DelayBySeconds(duration, () =>
                {
                    CloseCustomHud(playerId);
                });
                _menuCloseTimers[playerId] = cts;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] DisplayCustomHudStats error: {ex.Message}. Falling back to BuiltIn menu.");
            DisplayBuiltinStatsMenu(player, rawMessage, duration);
        }
    }
}

