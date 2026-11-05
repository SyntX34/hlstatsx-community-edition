using System;
using System.Collections.Generic;
using System.Linq;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
    private sealed class CustomMenuState
    {
        public string Title { get; set; } = "";
        public List<(string Label, Action? Action, bool IsInteractive)> Items { get; set; } = new();
        public int Page { get; set; }
        public int HoveredIndex { get; set; }
        public bool IsStatsView { get; set; }
        public int ItemsPerPage => 7;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(Items.Count / (double)ItemsPerPage));
    }

    private readonly Dictionary<int, CustomMenuState> _customMenuState = new();
    private readonly List<Guid> _numericKeyCommandGuids = new();

    private void RegisterCustomMenuHandlers()
    {
        Core.Event.OnClientKeyStateChanged -= HandleCustomMenuKeyState;
        Core.Event.OnClientKeyStateChanged += HandleCustomMenuKeyState;

        UnregisterNumericKeyCommands();
        for (int i = 1; i <= 9; i++)
        {
            int keyNum = i;
            try
            {
                var guid = Core.Command.RegisterCommand(keyNum.ToString(), (ctx) =>
                {
                    if (!ctx.IsSentByPlayer || ctx.Sender is not { } player || !player.IsValid) return;
                    HandleNumericKeyCommand(player, keyNum);
                });
                _numericKeyCommandGuids.Add(guid);
            }
            catch { }
        }
    }

    private void UnregisterCustomMenuHandlers()
    {
        Core.Event.OnClientKeyStateChanged -= HandleCustomMenuKeyState;
        UnregisterNumericKeyCommands();
    }

    private void UnregisterNumericKeyCommands()
    {
        foreach (var guid in _numericKeyCommandGuids)
        {
            try { Core.Command.UnregisterCommand(guid); } catch { }
        }
        _numericKeyCommandGuids.Clear();
    }

    private void HandleNumericKeyCommand(IPlayer player, int key)
    {
        int mode = _config.MenuType;
        if (mode != 2) return; /* KeyPress navigation mode */

        int playerId = player.PlayerID;
        if (!_playerHuds.TryGetValue(playerId, out var hud) || hud == null || !hud.IsValid) return;
        if (!_customMenuState.TryGetValue(playerId, out var state)) return;

        if (key >= 1 && key <= 7)
        {
            int index = (state.Page * state.ItemsPerPage) + (key - 1);
            if (index >= 0 && index < state.Items.Count)
            {
                var item = state.Items[index];
                if (item.IsInteractive && item.Action != null)
                {
                    Core.Scheduler.NextTick(() => item.Action.Invoke());
                }
            }
            return;
        }

        if (key == 7 && state.TotalPages > 1)
        {
            if (state.Page > 0)
            {
                state.Page--;
                RenderCustomMenu(player, hud, state);
            }
            else if (state.IsStatsView)
            {
                OpenCustomMenuMain(player);
            }
            return;
        }

        if (key == 8)
        {
            if (state.Page + 1 < state.TotalPages)
            {
                state.Page++;
                RenderCustomMenu(player, hud, state);
            }
            return;
        }

        if (key == 9)
        {
            CloseCustomHud(playerId);
        }
    }

    private void HandleCustomMenuKeyState(IOnClientKeyStateChangedEvent @event)
    {
        if (!@event.Pressed) return;
        int mode = _config.MenuType;
        if (mode != 4) return; /* Scrollable menu navigation */

        int playerId = @event.PlayerId;
        if (!_playerHuds.TryGetValue(playerId, out var hud) || hud == null || !hud.IsValid) return;
        if (!_customMenuState.TryGetValue(playerId, out var state)) return;

        var player = Core.PlayerManager.GetPlayer(playerId);
        if (player == null || !player.IsValid) return;

        if (@event.Key == KeyKind.W)
        {
            MoveScrollableMenuHover(player, hud, state, -1);
        }
        else if (@event.Key == KeyKind.S)
        {
            MoveScrollableMenuHover(player, hud, state, 1);
        }
        else if (@event.Key == KeyKind.E)
        {
            ExecuteScrollableMenuSelect(player, hud, state);
        }
        else if (@event.Key == KeyKind.R)
        {
            CloseCustomHud(playerId);
        }
    }

    private void MoveScrollableMenuHover(IPlayer player, CCSCustomHudLayout hud, CustomMenuState state, int delta)
    {
        int pageStart = state.Page * state.ItemsPerPage;
        int countOnPage = Math.Min(state.ItemsPerPage, state.Items.Count - pageStart);
        if (countOnPage <= 0) return;

        int newIndex = state.HoveredIndex + delta;
        if (newIndex < 0) newIndex = countOnPage - 1;
        else if (newIndex >= countOnPage) newIndex = 0;

        state.HoveredIndex = newIndex;
        RenderCustomMenu(player, hud, state);
    }

    private void ExecuteScrollableMenuSelect(IPlayer player, CCSCustomHudLayout hud, CustomMenuState state)
    {
        int index = (state.Page * state.ItemsPerPage) + state.HoveredIndex;
        if (index >= 0 && index < state.Items.Count)
        {
            var item = state.Items[index];
            if (item.IsInteractive && item.Action != null)
            {
                Core.Scheduler.NextTick(() => item.Action.Invoke());
            }
        }
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

    private void OpenCustomMenuMain(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            var hud = EnsureCustomHud(playerId);

            var loc = Core.Translation.GetPlayerLocalizer(player);
            string title = loc["hlx.menu_title"] ?? "► HLstatsX:CE Stats";

            var items = new List<(string Label, Action? Action, bool IsInteractive)>
            {
                ("My Rank",          () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "rank", "say"); }, true),
                ("Top 10 Players",   () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "top10", "say"); }, true),
                ("Top 20 Players",   () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "top20", "say"); }, true),
                ("Next Above Me",    () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "next", "say"); }, true),
                ("Session Stats",    () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "session", "say"); }, true),
                ("My Stats",         () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "statsme", "say"); }, true),
                ("Weapon Usage",     () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "weapons", "say"); }, true),
                ("Weapon Accuracy",  () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "accuracy", "say"); }, true),
                ("Hitgroup Targets", () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "targets", "say"); }, true),
                ("Top Victims",      () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "kills", "say"); }, true),
                ("Action Summary",   () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "actions", "say"); }, true),
                ("Map Stats",        () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "maps", "say"); }, true),
                ("Top Clans",        () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "clans", "say"); }, true),
                ("Server Status",    () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "status", "say"); }, true),
                ("Toggle Ranking",   () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "hlx_hideranking", "say"); }, true),
                ("Help & Commands",  () => { CloseCustomHud(playerId); DispatchPlayerStatCommand(player, "help", "say"); }, true)
            };

            var state = new CustomMenuState
            {
                Title = title,
                Items = items,
                Page = 0,
                HoveredIndex = 0,
                IsStatsView = false
            };

            _customMenuState[playerId] = state;
            RenderCustomMenu(player, hud, state);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] OpenCustomMenuMain error: {ex.Message}. Falling back to BuiltIn menu.");
            OpenBuiltinMenu(player);
        }
    }

    private void DisplayCustomStatsMenu(IPlayer player, string rawMessage, int duration = 15)
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

            if (optionLines.Count == 1 && title.Contains("Next Players", StringComparison.OrdinalIgnoreCase))
            {
                optionLines.Add("You are #1 - no players ranked above you!");
            }

            var items = new List<(string Label, Action? Action, bool IsInteractive)>();
            foreach (var opt in optionLines)
            {
                items.Add((opt, null, false));
            }

            var state = new CustomMenuState
            {
                Title = title,
                Items = items,
                Page = 0,
                HoveredIndex = 0,
                IsStatsView = true
            };

            _customMenuState[playerId] = state;
            RenderCustomMenu(player, hud, state);

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
            Console.WriteLine($"[HLstatsX:CE] DisplayCustomStatsMenu error: {ex.Message}. Falling back to BuiltIn menu.");
            DisplayBuiltinStatsMenu(player, rawMessage, duration);
        }
    }

    private void RenderCustomMenu(IPlayer player, CCSCustomHudLayout hud, CustomMenuState state)
    {
        int playerId = player.PlayerID;
        int mode = _config.MenuType;

        string pageInfo = state.TotalPages > 1 ? $"{state.Title} ({state.Page + 1}/{state.TotalPages})" : state.Title;
        hud.SetDialogVariableString("HlxMenuTitle", "menu_title", pageInfo);

        int startIndex = state.Page * state.ItemsPerPage;
        for (int i = 1; i <= 10; i++)
        {
            string key = $"HlxMenuOption{i:D2}";
            string labelKey = $"HlxMenuOption{i:D2}Label";
            string varName = $"option_{i:D2}";
            int itemIndex = startIndex + (i - 1);

            if (i <= state.ItemsPerPage && itemIndex < state.Items.Count)
            {
                var item = state.Items[itemIndex];
                string prefix = "";
                if (mode == 2 && item.IsInteractive)
                {
                    prefix = $"{i}. ";
                }
                else if (mode == 4)
                {
                    prefix = (i - 1 == state.HoveredIndex) ? "► " : "   ";
                }

                hud.SetDialogVariableString(labelKey, varName, $"{prefix}{item.Label}");
                SetHudClass(hud, key, "Hidden", false);
                SetHudClass(hud, key, "Interactive", mode == 3 && item.IsInteractive);
                SetHudClass(hud, key, "Hovered", mode == 4 && (i - 1 == state.HoveredIndex));
                SetHudClass(hud, key, "Selected", mode == 4 && (i - 1 == state.HoveredIndex));
            }
            else
            {
                hud.SetDialogVariableString(labelKey, varName, "");
                SetHudClass(hud, key, "Hidden", true);
                SetHudClass(hud, key, "Interactive", false);
                SetHudClass(hud, key, "Hovered", false);
                SetHudClass(hud, key, "Selected", false);
            }
        }

        string backText = state.Page > 0 ? (mode == 2 ? "7. Prev" : "Prev") : (state.IsStatsView ? (mode == 2 ? "7. Menu" : "Menu") : (mode == 2 ? "7. Back" : "Back"));
        string nextText = mode == 2 ? "8. Next" : "Next";
        string exitText = mode == 2 ? "9. Exit" : (mode == 4 ? "[R] Exit" : "Exit");

        hud.SetDialogVariableString("HlxMenuBackLabel", "back_text", backText);
        hud.SetDialogVariableString("HlxMenuNextLabel", "next_text", nextText);
        hud.SetDialogVariableString("HlxMenuExitLabel", "exit_text", exitText);

        bool showBack = state.Page > 0 || state.IsStatsView;
        SetHudClass(hud, "HlxMenuBack", "Hidden", !showBack);
        SetHudClass(hud, "HlxMenuBack", "Interactive", mode == 3 && showBack);

        bool showNext = state.Page + 1 < state.TotalPages;
        SetHudClass(hud, "HlxMenuNext", "Hidden", !showNext);
        SetHudClass(hud, "HlxMenuNext", "Interactive", mode == 3 && showNext);

        SetHudClass(hud, "HlxMenuExit", "Hidden", false);
        SetHudClass(hud, "HlxMenuExit", "Interactive", mode == 3);

        if (mode == 4)
        {
            hud.SetDialogVariableString("HlxMenuNavigateKeys", "navigate_keys", "W / S");
            hud.SetDialogVariableString("HlxMenuNavigateText", "navigate_text", "Navigate");
            hud.SetDialogVariableString("HlxMenuSelectKey", "select_key", "E");
            hud.SetDialogVariableString("HlxMenuSelectText", "select_text", "Select");
            hud.SetDialogVariableString("HlxMenuExitKey", "exit_key", "R");
            hud.SetDialogVariableString("HlxMenuExitText", "controls_exit_text", "Exit");
            SetHudClass(hud, "HlxMenuControls", "Hidden", false);
        }
        else
        {
            SetHudClass(hud, "HlxMenuControls", "Hidden", true);
        }

        /* Mouse capture for clickable menu */
        hud.SetInputCaptureEnabledForPlayer(playerId, mode == 3);
        SetHudClass(hud, "HlxMenuPanel", "Visible", true);
        _activeHudPlayers.Add(playerId);
    }

    private void HandleCustomHudClick(IOnCustomHudClickedEvent @event)
    {
        try
        {
            int playerId = @event.PlayerId;
            CCSCustomHudLayout? hud = null;
            if (!_playerHuds.TryGetValue(playerId, out hud) || hud == null || !hud.IsValid)
            {
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

            if (!_customMenuState.TryGetValue(playerId, out var state)) return;

            if (button.StartsWith("HlxMenuOption", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(button.Substring("HlxMenuOption".Length), out int optNum) && optNum >= 1 && optNum <= state.ItemsPerPage)
                {
                    int index = (state.Page * state.ItemsPerPage) + (optNum - 1);
                    if (index >= 0 && index < state.Items.Count)
                    {
                        var item = state.Items[index];
                        if (item.IsInteractive && item.Action != null)
                        {
                            Core.Scheduler.NextTick(() => item.Action.Invoke());
                        }
                    }
                }
                return;
            }

            if (button == "HlxMenuBack")
            {
                if (state.Page > 0)
                {
                    state.Page--;
                    Core.Scheduler.NextTick(() => RenderCustomMenu(player, hud, state));
                }
                else if (state.IsStatsView)
                {
                    Core.Scheduler.NextTick(() => OpenCustomMenuMain(player));
                }
                return;
            }

            if (button == "HlxMenuNext")
            {
                if (state.Page + 1 < state.TotalPages)
                {
                    state.Page++;
                    Core.Scheduler.NextTick(() => RenderCustomMenu(player, hud, state));
                }
                return;
            }

            if (button == "HlxMenuExit")
            {
                Core.Scheduler.NextTick(() => CloseCustomHud(playerId));
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] HandleCustomHudClick error: {ex.Message}");
        }
    }
}

