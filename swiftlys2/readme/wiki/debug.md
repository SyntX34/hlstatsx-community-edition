# HLStatsX:CE (SwiftlyS2) Debug & Troubleshooting

> [Back to README](../../HLStatsX/README.md) · [Installation Guide](installation.md)

## 1. Enable Debug Logging

Set `Debug = true` in `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml` and reload the plugin or restart the server.

## 2. Checking UDP Communication

Type `!rank` in game chat and watch the server console:

- **You see `UDP TX` but NO `UDP RX`:**
  - Verify `hlstats.pl` is running and listening on `DaemonHost:DaemonPort` (default `27500`).
  - Verify your firewall allows UDP traffic between `DaemonPort` (`27500`) and `ReceiverPort` (`27016`).
  - Verify `ServerIp`, `ServerPort`, and `Mod = SWIFTLYS2` match in HLstatsX Web Admin.

## 3. Custom Panorama HUD Not Showing (`MenuType = 2, 3, 4`)

- Confirm Steam Workshop item **`3791366113`** is mounted on the server and downloaded by clients.
- Temporarily set `MenuType = 1` to verify stats commands work with the built-in menu.

## 4. Command Collisions

- All plugin console commands use the `sw_` prefix (`registerRaw: false`), so they never collide with built-in Source 2 engine commands (`skill`, `kill`, `status`, `load`, etc.).
- Chat triggers (`!rank`, `!hlx`, `!mapstats`, etc.) are intercepted via `HookClientChat`.
