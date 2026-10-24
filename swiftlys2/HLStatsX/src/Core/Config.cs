using System;
using System.Collections.Generic;
using System.IO;
using SwiftlyS2.Shared.Plugins;

namespace HLStatsX;

public partial class HLStatsX : BasePlugin
{
    public class HLStatsXConfig
    {
        public bool Debug { get; set; } = false;
        public string DaemonHost { get; set; } = "127.0.0.1";
        public int DaemonPort { get; set; } = 27500;
        public string ServerIp { get; set; } = "5.135.143.217";
        public int ServerPort { get; set; } = 27015;
        public string ProxyKey { get; set; } = "";
        public int MaxPlayers { get; set; } = 32;
        public int ReceiverPort { get; set; } = 27016;
        public string MenuType { get; set; } = "BuiltIn";
        public string CustomMenuLayout { get; set; } = "panorama/layout/custom_game/hlx_menu.xml";
        public string CustomCsayLayout { get; set; } = "panorama/layout/custom_game/hlx_csay.xml";
        public string CustomTsayLayout { get; set; } = "panorama/layout/custom_game/hlx_tsay.xml";
        public int AutoCloseMenuSeconds { get; set; } = 15;
    }

    private HLStatsXConfig _config = new();

    private static string NormalizeLayoutPath(string? path, string defaultPath)
    {
        if (string.IsNullOrWhiteSpace(path)) return defaultPath;
        var cleaned = path.Trim().Trim('"', '\'').Replace('\\', '/');
        if (cleaned.StartsWith("file://{resources}/", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring("file://{resources}/".Length);
        if (cleaned.StartsWith("resources/", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring("resources/".Length);
        if (cleaned.StartsWith("layout/", StringComparison.OrdinalIgnoreCase))
            cleaned = "panorama/" + cleaned;
        return cleaned;
    }

    private void LogDebug(string message)
    {
        if (_config.Debug)
        {
            Console.WriteLine($"[HLstatsX:CE DEBUG] {message}");
        }
    }

    private void LoadConfig()
    {
        try
        {
            var cfgService = Core.Configuration
                .InitializeTomlWithModel<HLStatsXConfig>("config.toml", "HLStatsX");
            var section = cfgService.Manager.GetSection("HLStatsX");
            var debugStr = section["Debug"];
            var host = section["DaemonHost"];
            var port = section["DaemonPort"];
            var sip = section["ServerIp"];
            var sport = section["ServerPort"];
            var pkey = section["ProxyKey"];
            if (!string.IsNullOrWhiteSpace(debugStr) && bool.TryParse(debugStr, out bool dbg)) _config.Debug = dbg;
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
                    candidates.Add(Path.Combine(Core.Configuration.BasePath, "config.toml"));
                    candidates.Add(Path.Combine(Core.Configuration.BasePath, "HLStatsX", "config.toml"));
                }
            }
            catch { }
            candidates.Add(Path.Combine("addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(Path.Combine("csgo", "addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "configs", "plugins", "HLStatsX", "config.toml"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "addons", "swiftlys2", "configs", "plugins", "HLStatsX", "config.toml"));

            string? foundPath = null;
            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c) && File.Exists(c))
                {
                    foundPath = c;
                    break;
                }
            }

            if (foundPath != null)
            {
                Console.WriteLine($"[HLstatsX:CE] Reading config file from: {foundPath}");
                foreach (var line in File.ReadAllLines(foundPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith('#') || trimmed.StartsWith(';') || !trimmed.Contains('=')) continue;
                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();
                    int commentIdx = val.IndexOf('#');
                    if (commentIdx >= 0 && !val.StartsWith('"') && !val.StartsWith('\''))
                    {
                        val = val.Substring(0, commentIdx).Trim();
                    }
                    val = val.Trim('"', '\'');

                    if (key.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(val, out bool dbg)) _config.Debug = dbg;
                        else if (val == "1") _config.Debug = true;
                        else if (val == "0") _config.Debug = false;
                    }
                    else if (key.Equals("DaemonHost", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.DaemonHost = val;
                    else if (key.Equals("DaemonPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int p)) _config.DaemonPort = p;
                    else if (key.Equals("ServerIp", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.ServerIp = val;
                    else if (key.Equals("ServerPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int sp)) _config.ServerPort = sp;
                    else if (key.Equals("ProxyKey", StringComparison.OrdinalIgnoreCase)) _config.ProxyKey = val;
                    else if (key.Equals("MaxPlayers", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int mp)) _config.MaxPlayers = mp;
                    else if (key.Equals("ReceiverPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int rp)) _config.ReceiverPort = rp;
                    else if (key.Equals("MenuType", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.MenuType = val;
                    else if (key.Equals("CustomMenuLayout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.CustomMenuLayout = val;
                    else if (key.Equals("CustomCsayLayout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.CustomCsayLayout = val;
                    else if (key.Equals("CustomTsayLayout", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val)) _config.CustomTsayLayout = val;
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

        _config.CustomMenuLayout = NormalizeLayoutPath(_config.CustomMenuLayout, "panorama/layout/custom_game/hlx_menu.xml");
        _config.CustomCsayLayout = NormalizeLayoutPath(_config.CustomCsayLayout, "panorama/layout/custom_game/hlx_csay.xml");
        _config.CustomTsayLayout = NormalizeLayoutPath(_config.CustomTsayLayout, "panorama/layout/custom_game/hlx_tsay.xml");

        LogDebug($"Configuration loaded: Daemon={_config.DaemonHost}:{_config.DaemonPort}, Server={_config.ServerIp}:{_config.ServerPort}, ReceiverPort={_config.ReceiverPort}, MenuType={_config.MenuType}, MenuLayout={_config.CustomMenuLayout}");
    }
}

