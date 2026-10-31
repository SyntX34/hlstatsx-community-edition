using System;
using System.Collections.Generic;
using System.Linq;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rank", "skill", "points", "place", "session", "session_data",
        "kpd", "kdratio", "kdeath", "next", "load", "status", "servers",
        "top", "top20", "top15", "top12", "top10", "top5", "clans", "bans", "cheaters", "statsme",
        "weapons", "weapon", "weapon_usage", "action", "actions", "accuracy", "targets",
        "target", "kills", "kill", "player_kills", "map", "maps", "map_stats",
        "cmd", "cmds", "command", "commands",
        "hlx_display 0", "hlx_display 1", "hlx_teams 0", "hlx_teams 1",
        "hlx_hideranking", "hlx_chat 0", "hlx_chat 1", "hlx_menu",
        "servers 1", "servers 2", "servers 3", "hlx", "hlstatsx", "help"
    };

    private static bool IsStatCommand(string cmd, string fullCleaned)
    {
        if (BlockedCommands.Contains(fullCleaned) || BlockedCommands.Contains(cmd)) return true;
        if (cmd.StartsWith("top", StringComparison.OrdinalIgnoreCase)) return true;
        if (cmd.StartsWith("hlx_", StringComparison.OrdinalIgnoreCase)) return true;
        if (WeaponCode.ContainsKey(cmd)) return true;
        return false;
    }

    private static readonly HashSet<string> MenuCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "hlx", "hlstatsx", "menu", "hlxmenu", "hlx_menu", "statsmenu"
    };

    private void RegisterServerCommands()
    {
        Core.Command.RegisterCommand("hlx_sm_psay", (context) =>
        {
            if (context.Args.Length == 0) return;
            if (context.Args.Length == 1)
            {
                string text = context.Args[0].Trim('"', '\'');
                foreach (var player in Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient))
                    player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, text));
                return;
            }
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
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_psay2", (context) =>
        {
            if (context.Args.Length < 2) return;
            var player = FindPlayerTarget(context.Args[0]);
            string message = string.Join(" ", context.Args.Skip(1));
            if (player != null && player.IsValid && !player.IsFakeClient)
                player.SendMessage(MessageType.Chat, FormatSourceModMessage(player, message));
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_csay", (context) =>
        {
            if (context.Args.Length < 1) return;
            string message = string.Join(" ", context.Args).Trim('"', '\'');
            ShowCenterHud(null, message, 4f);
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_tsay", (context) =>
        {
            if (context.Args.Length < 2) return;
            string targetUserIdStr = context.Args.Length >= 3 ? context.Args[1] : context.Args[0];
            string message = (context.Args.Length >= 3
                ? string.Join(" ", context.Args.Skip(2))
                : string.Join(" ", context.Args.Skip(1))).Trim('"', '\'');

            if (targetUserIdStr.Equals("0") || targetUserIdStr.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                ShowTopLeftHud(null, message, 4f);
            }
            else
            {
                var player = FindPlayerTarget(targetUserIdStr);
                if (player != null && player.IsValid && !player.IsFakeClient)
                    ShowTopLeftHud(player, message, 4f);
            }
        }, registerRaw: true);

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
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_msay", (context) =>
        {
            if (context.Args.Length < 2) return;
            string payload = string.Join(" ", context.Args);
            HandleMsayPayload(payload);
        }, registerRaw: true);

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
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_player_action", (context) =>
        {
            if (context.Args.Length < 2) return;
            var player = FindPlayerTarget(context.Args[0]);
            if (player != null && player.IsValid)
                SendLog(player, context.Args[1], "triggered");
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_team_action", (context) =>
        {
            if (context.Args.Length < 2) return;
            SendLog(null, $"Team \"{context.Args[0]}\" triggered \"{context.Args[1]}\"", null);
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_sm_world_action", (context) =>
        {
            if (context.Args.Length < 1) return;
            SendLog(null, $"World triggered \"{context.Args[0]}\"", null);
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_protect_address", (context) =>
        {
            if (context.Args.Length > 0)
                _protectAddress = context.Args[0];
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_block_commands", (context) =>
        {
            if (context.Args.Length > 0 && int.TryParse(context.Args[0], out int val))
                _blockChatCommands = (val == 1);
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_message_prefix", (context) =>
        {
            if (context.Args.Length > 0)
                _messagePrefix = string.Join(" ", context.Args);
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlx_message_prefix_clear", (context) =>
        {
            _messagePrefix = "";
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlxce_webpage", (context) =>
        {
            if (context.Args.Length > 0)
                _webpageUrl = string.Join(" ", context.Args).Trim('"', '\'');
        }, registerRaw: true);

        Core.Command.RegisterCommand("hlxce_version", (context) =>
        {
            if (context.Args.Length > 0)
                _hlxceVersion = string.Join(" ", context.Args).Trim('"', '\'');
        }, registerRaw: true);

        void RegisterPlayerStatsCommand(string name)
        {
            Core.Command.RegisterCommand(name, (context) =>
            {
                if (!context.IsSentByPlayer || context.Sender is not { } player || !player.IsValid) return;
                string arg = context.Args.Length > 0 ? " " + string.Join(" ", context.Args) : "";
                SendLog(player, $"{name}{arg}", "say");
            }, registerRaw: true);
            try { Core.Command.RegisterCommandAlias(name, $"!{name}"); } catch { }
            try { Core.Command.RegisterCommandAlias(name, $"/{name}"); } catch { }
        }

        RegisterPlayerStatsCommand("rank");
        RegisterPlayerStatsCommand("skill");
        RegisterPlayerStatsCommand("points");
        RegisterPlayerStatsCommand("place");
        RegisterPlayerStatsCommand("top");
        RegisterPlayerStatsCommand("top10");
        RegisterPlayerStatsCommand("top12");
        RegisterPlayerStatsCommand("top15");
        RegisterPlayerStatsCommand("top20");
        RegisterPlayerStatsCommand("top5");
        RegisterPlayerStatsCommand("statsme");
        RegisterPlayerStatsCommand("session");
        RegisterPlayerStatsCommand("session_data");
        RegisterPlayerStatsCommand("next");
        RegisterPlayerStatsCommand("kpd");
        RegisterPlayerStatsCommand("kdratio");
        RegisterPlayerStatsCommand("kdeath");
        RegisterPlayerStatsCommand("weapon");
        RegisterPlayerStatsCommand("weapons");
        RegisterPlayerStatsCommand("weapon_usage");
        RegisterPlayerStatsCommand("accuracy");
        RegisterPlayerStatsCommand("target");
        RegisterPlayerStatsCommand("targets");
        RegisterPlayerStatsCommand("kill");
        RegisterPlayerStatsCommand("kills");
        RegisterPlayerStatsCommand("player_kills");
        RegisterPlayerStatsCommand("action");
        RegisterPlayerStatsCommand("actions");
        RegisterPlayerStatsCommand("map");
        RegisterPlayerStatsCommand("maps");
        RegisterPlayerStatsCommand("map_stats");
        RegisterPlayerStatsCommand("clans");
        RegisterPlayerStatsCommand("cheaters");
        RegisterPlayerStatsCommand("bans");
        RegisterPlayerStatsCommand("status");
        RegisterPlayerStatsCommand("load");
        RegisterPlayerStatsCommand("servers");
        RegisterPlayerStatsCommand("help");
        RegisterPlayerStatsCommand("cmd");
        RegisterPlayerStatsCommand("cmds");
        RegisterPlayerStatsCommand("commands");
        RegisterPlayerStatsCommand("hlx_hideranking");
        RegisterPlayerStatsCommand("hlx_display");
        RegisterPlayerStatsCommand("hlx_chat");
        RegisterPlayerStatsCommand("hlx_teams");
        RegisterPlayerStatsCommand("hlx_set");
        RegisterPlayerStatsCommand("hlx_auto");

        void RegisterMenuCommand(string name)
        {
            Core.Command.RegisterCommand(name, (context) =>
            {
                if (!context.IsSentByPlayer || context.Sender is not { } player || !player.IsValid) return;
                OpenStatsMenu(player);
            }, registerRaw: true);
            try { Core.Command.RegisterCommandAlias(name, $"!{name}"); } catch { }
            try { Core.Command.RegisterCommandAlias(name, $"/{name}"); } catch { }
        }

        RegisterMenuCommand("hlx");
        RegisterMenuCommand("hlstatsx");
        RegisterMenuCommand("menu");
        RegisterMenuCommand("hlxmenu");
        RegisterMenuCommand("hlx_menu");
        RegisterMenuCommand("statsmenu");
    }
}

