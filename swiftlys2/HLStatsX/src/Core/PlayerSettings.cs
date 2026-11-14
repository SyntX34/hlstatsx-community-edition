using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
    public sealed class PlayerChatSettings
    {
        public string ChatMode { get; set; } = "chat";
        public long MuteUntilUtc { get; set; } = 0;
        public bool MuteUntilMapChange { get; set; } = false;
    }

    private readonly Dictionary<string, PlayerChatSettings> _playerSettings = new(StringComparer.OrdinalIgnoreCase);
    private string _settingsFilePath = "";

    private static string GetSteamId2(ulong steamId64)
    {
        if (steamId64 < 76561197960265728UL) return steamId64.ToString();
        ulong accountId = steamId64 - 76561197960265728UL;
        ulong y = accountId & 1UL;
        ulong z = accountId >> 1;
        return $"STEAM_0:{y}:{z}";
    }

    private string ResolveSettingsFilePath()
    {
        if (!string.IsNullOrEmpty(_settingsFilePath)) return _settingsFilePath;

        try
        {
            string cfgPath = Core.Configuration.GetConfigPath("config.toml");
            if (!string.IsNullOrWhiteSpace(cfgPath))
            {
                string? dir = Path.GetDirectoryName(cfgPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _settingsFilePath = Path.Combine(dir, "settings.jsonc");
                    return _settingsFilePath;
                }
            }
        }
        catch { }

        _settingsFilePath = Path.Combine("addons", "swiftlys2", "configs", "plugins", "HLStatsX", "settings.jsonc");
        return _settingsFilePath;
    }

    private void LoadPlayerSettings()
    {
        try
        {
            string path = ResolveSettingsFilePath();
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, PlayerChatSettings>>(json, new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            _playerSettings.Clear();
            if (loaded != null)
            {
                foreach (var kvp in loaded)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                        _playerSettings[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Failed to load settings.jsonc: {ex.Message}");
        }
    }

    private void SavePlayerSettings()
    {
        try
        {
            string path = ResolveSettingsFilePath();
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(_playerSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] Failed to save settings.jsonc: {ex.Message}");
        }
    }

    private PlayerChatSettings GetOrCreatePlayerSettings(IPlayer player)
    {
        string steam2 = GetSteamId2(player.SteamID);
        if (!_playerSettings.TryGetValue(steam2, out var settings))
        {
            settings = new PlayerChatSettings();
            _playerSettings[steam2] = settings;
        }
        return settings;
    }

    private void ResetMapChangeMutes()
    {
        bool changed = false;
        foreach (var kvp in _playerSettings)
        {
            if (kvp.Value.MuteUntilMapChange)
            {
                kvp.Value.MuteUntilMapChange = false;
                changed = true;
            }
        }
        if (changed) SavePlayerSettings();
    }

    private bool IsPlayerChatMuted(IPlayer player)
    {
        if (player == null || !player.IsValid || player.IsFakeClient) return false;
        string steam2 = GetSteamId2(player.SteamID);
        if (!_playerSettings.TryGetValue(steam2, out var s)) return false;

        if (s.MuteUntilMapChange) return true;
        if (s.MuteUntilUtc == -1) return true;
        if (s.MuteUntilUtc > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < s.MuteUntilUtc) return true;

            s.MuteUntilUtc = 0;
            SavePlayerSettings();
        }
        return false;
    }

    private string GetPlayerChatMode(IPlayer player)
    {
        if (player == null || !player.IsValid || player.IsFakeClient) return "chat";
        string steam2 = GetSteamId2(player.SteamID);
        if (_playerSettings.TryGetValue(steam2, out var s) &&
            s.ChatMode.Equals("console", StringComparison.OrdinalIgnoreCase))
        {
            return "console";
        }
        return "chat";
    }

    private string GetMuteStatusLabel(PlayerChatSettings s)
    {
        if (s.MuteUntilMapChange) return "Until Map Change";
        if (s.MuteUntilUtc == -1) return "Permanent";
        if (s.MuteUntilUtc > 0)
        {
            long rem = s.MuteUntilUtc - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (rem > 0)
            {
                var ts = TimeSpan.FromSeconds(rem);
                if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h left";
                if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m left";
                return $"{Math.Max(1, (int)ts.TotalMinutes)}m left";
            }
        }
        return "Off";
    }

    private static readonly (string Key, string Fallback, bool MapChange, long Seconds)[] MuteDurations =
    {
        ("hlx.settings_mute_off",  "Unmute (Off)",     false, 0),
        ("hlx.settings_mute_map",  "Until Map Change", true,  0),
        ("hlx.settings_mute_10m",  "10 Minutes",       false, 10 * 60),
        ("hlx.settings_mute_20m",  "20 Minutes",       false, 20 * 60),
        ("hlx.settings_mute_30m",  "30 Minutes",       false, 30 * 60),
        ("hlx.settings_mute_1h",   "1 Hour",           false, 3600),
        ("hlx.settings_mute_3h",   "3 Hours",          false, 3 * 3600),
        ("hlx.settings_mute_1d",   "1 Day",            false, 86400),
        ("hlx.settings_mute_3d",   "3 Days",           false, 3 * 86400),
        ("hlx.settings_mute_1w",   "1 Week",           false, 7 * 86400),
        ("hlx.settings_mute_1mo",  "1 Month",          false, 30L * 86400),
        ("hlx.settings_mute_perm", "Permanent",        false, -1)
    };

    private string T(IPlayer player, string key, string fallback, params object[] args)
    {
        try
        {
            var loc = Core.Translation.GetPlayerLocalizer(player);
            string raw = loc[key];
            if (!string.IsNullOrWhiteSpace(raw) && !raw.Equals(key, StringComparison.Ordinal))
                return args.Length > 0 ? string.Format(raw, args) : raw;
        }
        catch { }
        return args.Length > 0 ? string.Format(fallback, args) : fallback;
    }

    private void ApplyMuteDuration(IPlayer player, bool mapChange, long seconds, string label)
    {
        var s = GetOrCreatePlayerSettings(player);
        s.MuteUntilMapChange = mapChange;
        if (mapChange)
            s.MuteUntilUtc = 0;
        else if (seconds == -1)
            s.MuteUntilUtc = -1;
        else if (seconds == 0)
            s.MuteUntilUtc = 0;
        else
            s.MuteUntilUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;

        SavePlayerSettings();
        player.SendMessage(MessageType.Chat,
            FormatColors(T(player, "hlx.settings_mute_set", "[gold][HLstatsX:CE][default] Chat messages mute set to: [green]{0}", label)));
    }

    private void OpenSettingsMenu(IPlayer player)
    {
        if (player == null || !player.IsValid) return;
        ScheduleMenuAutoClose(player);

        if (_config.MenuType >= 2 && _config.MenuType <= 4)
        {
            OpenCustomSettingsMenu(player);
            return;
        }

        OpenBuiltinSettingsMenu(player);
    }

    private void OpenBuiltinSettingsMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            DisposePlayerMenu(playerId);

            var s = GetOrCreatePlayerSettings(player);
            string modeLabel = s.ChatMode.Equals("console", StringComparison.OrdinalIgnoreCase)
                ? T(player, "hlx.settings_mode_console", "Console")
                : T(player, "hlx.settings_mode_chat", "Chat");
            string muteLabel = GetMuteStatusLabel(s);

            var builder = Core.Menu.CreateMenu($"hlstatsx.settings.{playerId}")
                .WithTitle(T(player, "hlx.settings_title", "► HLstatsX:CE Settings"))
                .WithItemsPerPage(5);

            if (_config.AutoCloseMenuSeconds > 0)
                builder.WithAutoClose(TimeSpan.FromSeconds(_config.AutoCloseMenuSeconds));

            builder.Add(new ButtonComponent(T(player, "hlx.settings_chat_mode", "Chat Mode: {0}", modeLabel), ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() =>
                    {
                        var st = GetOrCreatePlayerSettings(p);
                        st.ChatMode = st.ChatMode.Equals("console", StringComparison.OrdinalIgnoreCase) ? "chat" : "console";
                        SavePlayerSettings();
                        OpenBuiltinSettingsMenu(p);
                    });
                }
                return ValueTask.CompletedTask;
            }) { CloseOnSelect = true });

            builder.Add(new ButtonComponent(T(player, "hlx.settings_block_chat", "Block Chat: {0}", muteLabel), ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() => OpenBuiltinMuteDurationMenu(p));
                }
                return ValueTask.CompletedTask;
            }) { CloseOnSelect = true });

            builder.Add(new ButtonComponent(T(player, "hlx.settings_back_main", "« Main Menu"), ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() => OpenStatsMenu(p));
                }
                return ValueTask.CompletedTask;
            }) { CloseOnSelect = true });

            var menu = builder.Build();
            _playerMenus[playerId] = menu;
            menu.Open(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] OpenBuiltinSettingsMenu error: {ex.Message}");
        }
    }

    private void OpenBuiltinMuteDurationMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            DisposePlayerMenu(playerId);

            var builder = Core.Menu.CreateMenu($"hlstatsx.mute.{playerId}")
                .WithTitle(T(player, "hlx.settings_mute_title", "► Block Chat Duration"))
                .WithItemsPerPage(5);

            if (_config.AutoCloseMenuSeconds > 0)
                builder.WithAutoClose(TimeSpan.FromSeconds(_config.AutoCloseMenuSeconds));

            foreach (var dur in MuteDurations)
            {
                var d = dur;
                string dLabel = T(player, d.Key, d.Fallback);
                builder.Add(new ButtonComponent(dLabel, ctx =>
                {
                    var p = ctx.Player;
                    if (p != null && p.IsValid)
                    {
                        Core.Scheduler.NextTick(() =>
                        {
                            ApplyMuteDuration(p, d.MapChange, d.Seconds, dLabel);
                            OpenBuiltinSettingsMenu(p);
                        });
                    }
                    return ValueTask.CompletedTask;
                }) { CloseOnSelect = true });
            }

            builder.Add(new ButtonComponent(T(player, "hlx.settings_back_settings", "« Back to Settings"), ctx =>
            {
                var p = ctx.Player;
                if (p != null && p.IsValid)
                {
                    Core.Scheduler.NextTick(() => OpenBuiltinSettingsMenu(p));
                }
                return ValueTask.CompletedTask;
            }) { CloseOnSelect = true });

            var menu = builder.Build();
            _playerMenus[playerId] = menu;
            menu.Open(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLstatsX:CE] OpenBuiltinMuteDurationMenu error: {ex.Message}");
        }
    }

    private void OpenCustomSettingsMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            var hud = EnsureCustomHud(playerId);
            var s = GetOrCreatePlayerSettings(player);
            string modeLabel = s.ChatMode.Equals("console", StringComparison.OrdinalIgnoreCase)
                ? T(player, "hlx.settings_mode_console", "Console")
                : T(player, "hlx.settings_mode_chat", "Chat");
            string muteLabel = GetMuteStatusLabel(s);

            var items = new List<(string Label, Action? Action, bool IsInteractive)>
            {
                (T(player, "hlx.settings_chat_mode", "Chat Mode: {0}", modeLabel), () =>
                {
                    var st = GetOrCreatePlayerSettings(player);
                    st.ChatMode = st.ChatMode.Equals("console", StringComparison.OrdinalIgnoreCase) ? "chat" : "console";
                    SavePlayerSettings();
                    OpenCustomSettingsMenu(player);
                }, true),
                (T(player, "hlx.settings_block_chat", "Block Chat: {0}", muteLabel), () => OpenCustomMuteDurationMenu(player), true),
                (T(player, "hlx.settings_back_main", "« Main Menu"), () => OpenCustomMenuMain(player), true)
            };

            var state = new CustomMenuState
            {
                Title = T(player, "hlx.settings_title", "► HLstatsX:CE Settings"),
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
            Console.WriteLine($"[HLstatsX:CE] OpenCustomSettingsMenu error: {ex.Message}");
            OpenBuiltinSettingsMenu(player);
        }
    }

    private void OpenCustomMuteDurationMenu(IPlayer player)
    {
        try
        {
            int playerId = player.PlayerID;
            var hud = EnsureCustomHud(playerId);

            var items = new List<(string Label, Action? Action, bool IsInteractive)>();
            foreach (var dur in MuteDurations)
            {
                var d = dur;
                string dLabel = T(player, d.Key, d.Fallback);
                items.Add((dLabel, () =>
                {
                    ApplyMuteDuration(player, d.MapChange, d.Seconds, dLabel);
                    OpenCustomSettingsMenu(player);
                }, true));
            }
            items.Add((T(player, "hlx.settings_back_settings", "« Back to Settings"), () => OpenCustomSettingsMenu(player), true));

            var state = new CustomMenuState
            {
                Title = T(player, "hlx.settings_mute_title", "► Block Chat Duration"),
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
            Console.WriteLine($"[HLstatsX:CE] OpenCustomMuteDurationMenu error: {ex.Message}");
            OpenBuiltinMuteDurationMenu(player);
        }
    }
}
