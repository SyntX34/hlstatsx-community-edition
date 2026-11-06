using System;
using System.Collections.Generic;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
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
                        DispatchPlayerStatCommand(p, cmd, "say");
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
        AddCmdButton("Map Stats (!mapstats)",    "maps");
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

            if (optionLines.Count == 1 && title.Contains("Next Players", StringComparison.OrdinalIgnoreCase))
            {
                optionLines.Add("You are #1 - no players ranked above you!");
            }

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
}

