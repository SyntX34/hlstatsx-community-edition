# HLStatsX:CE (SwiftlyS2) — Setup & Debug Wiki

> **Back to README:** [English](../HLStatsX/README.md)

---

## 1. Prerequisites

- **Counter-Strike 2 Dedicated Server**
- **[MetaMod:Source](https://www.sourcemm.net/)** (v2.0+)
- **[SwiftlyS2 Framework](https://github.com/swiftly-solution/swiftlys2)**
- **.NET 10 Runtime** (matching your SwiftlyS2 build)
- **HLstatsX:CE Perl Daemon** (`hlstats.pl`) & MySQL/MariaDB

---

## 2. Installation

1. Download `HLStatsX-vX.X.X.zip` from [Releases](https://github.com/SyntX34/hlstatsx-community-edition/releases).
2. Extract into your CS2 server:
   ```text
   csgo/addons/swiftlys2/plugins/HLStatsX/
   ├── HLStatsX.dll
   ├── configs/HLStatsX/config.toml
   └── resources/
       ├── translations/
       └── panorama/
   ```
3. In `game/csgo/cfg/server.cfg`, ensure standard logging is enabled:
   ```cfg
   log on
   ```
   *(CS2 removed `logaddress_add`; the plugin streams logs directly to `hlstats.pl` over UDP.)*

---

## 3. Configuration (`config.toml`)

File location: `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml`

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

---

## 4. Custom Panorama HUD Workshop Requirement

For `MenuType = 2`, `3`, or `4` (and custom CSAY/TSAY banners), mount the Steam Workshop item on your CS2 server:
- **Workshop ID:** `3791366113` ([Steam Workshop Link](https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113))

---

## 5. HLstatsX Web Admin Setup

1. Open **HLstatsX Web Admin → Servers**.
2. Add or edit your CS2 server:
   - **IP / Port:** Must match `ServerIp` and `ServerPort` in `config.toml`.
   - **Mod:** Set to **`SWIFTLYS2`**.
   - **RCON / ReceiverPort:** Set to `ReceiverPort` (default `27016`, i.e., `ServerPort + 1`).
3. If behind NAT/Docker, set `ProxyKey` in both Web Admin and `config.toml`.

---

## 6. Debugging & Troubleshooting

1. Set `Debug = true` in `config.toml` and reload/restart the server.
2. Type `!rank` in game chat and watch the server console:
   - **You see `UDP TX` but NO `UDP RX`:**
     - Verify `hlstats.pl` is running and listening on `DaemonHost:DaemonPort` (`27500`).
     - Verify firewall allows UDP between `DaemonPort` (`27500`) and `ReceiverPort` (`27016`).
     - Verify `ServerIp`, `ServerPort`, and `Mod = SWIFTLYS2` match in HLstatsX Web Admin.
   - **Custom Panorama HUD doesn't show (`MenuType = 2, 3, 4`):**
     - Ensure Workshop item `3791366113` is mounted and clients downloaded it.
     - Temporarily set `MenuType = 1` to confirm stats commands work with the built-in menu.
   - **ConCommand collisions:**
     - All plugin commands use the `sw_` prefix (`registerRaw: false`), while chat triggers (`!rank`, `!hlx`, `!mapstats`, etc.) are intercepted via `HookClientChat`.
