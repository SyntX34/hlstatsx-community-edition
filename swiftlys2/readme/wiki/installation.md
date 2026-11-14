# HLStatsX:CE (SwiftlyS2) Installation Guide

> [Back to README](../../HLStatsX/README.md) · [Debug & Troubleshooting](debug.md)

## 1. Prerequisites

- Counter-Strike 2 Dedicated Server
- [MetaMod:Source](https://www.sourcemm.net/) (v2.0+)
- [SwiftlyS2 Framework](https://github.com/swiftly-solution/swiftlys2)
- .NET 10 Runtime (matching your SwiftlyS2 build)
- HLstatsX:CE Perl Daemon (`hlstats.pl`) and MySQL/MariaDB

## 2. Plugin Installation

1. Download `HLStatsX-vX.X.X.zip` from [Releases](https://github.com/SyntX34/hlstatsx-community-edition/releases).
2. Extract it into your CS2 server directory:
   ```text
   csgo/addons/swiftlys2/plugins/HLStatsX/
   ├── HLStatsX.dll
   ├── configs/HLStatsX/config.toml
   └── resources/
       ├── translations/
       └── panorama/
   ```
3. In `game/csgo/cfg/server.cfg`, make sure standard logging is enabled:
   ```cfg
   log on
   ```
   CS2 removed `logaddress_add`; the plugin streams logs directly to `hlstats.pl` over UDP.

## 3. Configuration (`config.toml`)

File path: `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml`

```toml
[HLStatsX]
Debug = false
DaemonHost = "127.0.0.1"
DaemonPort = 27500
ServerIp = "127.0.0.1"
ServerPort = 27015
ProxyKey = ""
MaxPlayers = 32
ReceiverPort = 27016
MenuType = 1
AutoCloseMenuSeconds = 15
CustomMenuLayout = "panorama/layout/custom_game/hlx_menu.xml"
CustomCsayLayout = "panorama/layout/custom_game/hlx_csay.xml"
CustomTsayLayout = "panorama/layout/custom_game/hlx_tsay.xml"
```

### `MenuType` Options

| Value | Mode | Controls |
| :---: | :--- | :--- |
| `1` | **Built-In** | SwiftlyS2 native `Core.Menu` (no workshop download needed) |
| `2` | **Keypress** | `1–7` select, `7/8` Prev/Next, `9` Exit |
| `3` | **Clickable** | Full mouse cursor interaction |
| `4` | **Scrollable** | `W`/`S` navigate, `A`/`D` Prev/Next page, `E` select, `R` exit |

## 4. Custom Panorama HUD Workshop Requirement

For `MenuType = 2`, `3`, or `4` (and custom CSAY/TSAY banners), mount Steam Workshop item **`3791366113`** on your CS2 server:
- [Steam Workshop Item 3791366113](https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113)

## 5. HLstatsX Web Admin Setup

1. Open **HLstatsX Web Admin -> Servers**.
2. Add or edit your CS2 server:
   - **IP / Port:** Must match `ServerIp` and `ServerPort` in `config.toml`.
   - **Mod:** Set to **`SWIFTLYS2`**.
   - **RCON / ReceiverPort:** Set to `ReceiverPort` (default `27016`, i.e., `ServerPort + 1`).
3. If behind NAT or Docker, set `ProxyKey` in both Web Admin and `config.toml`.
