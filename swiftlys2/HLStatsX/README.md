<div align="center">
  <h2><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h2>
  <h3>Counter-Strike 2 Real-Time Stats & Panorama HUD Integration</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>Author:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 Profile</a>
  </p>
</div>

---

## 📌 Overview

This plugin bridges **Counter-Strike 2** dedicated servers running the [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) framework with the **HLstatsX:CE** Perl backend daemon and web statistics interface.

It features complete bi-directional UDP communication, player stat tracking, ranking announces, in-game interactive Panorama menus, and HUD-based broadcast notifications (CSAY & TSAY).

---

## ✨ Features

- **🎮 Full Stats Tracking**: Weapon kills, headshots, hits, accuracy, hitgroups (head, chest, stomach, arms, legs), suicides, team kills, bomb plants/defuses, hostage events, MVPs, and live CS2 `ClanTag` synchronization.
- **💬 Chat Command Interception**: Intercepts all player commands (`!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!maps`, `!clans`, `!bans`, `!servers`, `!status`, `!load`, `!help`, weapon codes like `!ak47`, `!awp`, etc.), forwards them silently to the daemon, and suppresses spam in public chat.
- **📊 Native Scrollable `Core.Menu` & Panorama Custom HUD Menus**:
  - **BuiltIn (`MenuType = "1"`)**: Uses SwiftlyS2's native `Core.Menu` (`IMenuService`) with multi-page scrolling (`Shift` / `F` / `E` / `Tab`) and safe HTML escaping out of the box—no client downloads required.
  - **CustomHud (`MenuType = "2"`)**: Supports rich interactive Panorama HUD menus (`hlx_menu.xml`, `hlx_csay.xml`, `hlx_tsay.xml`) when the Metamod CustomHud workshop addon is installed.
- **🖥️ CSAY & TSAY Displays**:
  - `hlx_sm_csay`: Center screen announcement (native CS2 center message when `MenuType = "1"`, or CustomHud banner when `MenuType = "2"`).
  - `hlx_sm_tsay`: Top-left HUD / chat alert.
- **🔄 Multi-Map Socket & Player State Continuity**: Automatically restarts the UDP client/listener and re-announces connected players across map changes (`HandleMapLoad`, `round_start`, and `player_spawn`) so tracking never drops on the second map.
- **🔔 Connect & Event Announcements**: Automatically announces player connections with rank, points, and country geolocation.
- **🌐 In-Game Webpage Support**: Prints player profile or stats website URLs directly in chat (`hlxce_webpage`) since CS2 removed the legacy Source 1 MOTD browser.
- **🛡️ Address Protection**: Protects log forwarding addresses using `hlx_protect_address`.

---

## 📦 Steam Workshop Requirement

For custom Panorama HUD layouts (`hlx_menu.xml`, `hlx_csay.xml`, `hlx_tsay.xml`) and styles to render on player clients, subscribe and mount the official Steam Workshop asset:

> 🔗 **Steam Workshop Item**: [https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113](https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113)

Add `+host_workshop_map` or add workshop collection containing **`3791366113`** to your CS2 server startup parameters or mapcycle.

---

## 🛠️ Installation & Setup

### 1. Prerequisites
- **Counter-Strike 2 Dedicated Server**
- **[MetaMod:Source](https://www.sourcemm.net/)** (v2.0 or newer)
- **[SwiftlyS2 Framework](https://github.com/swiftly-solution/swiftlys2)** installed on the server
- **[.NET 10 Runtime](https://dotnet.microsoft.com/)** (matching SwiftlyS2 build)

### 2. Install the Plugin
1. Download the latest `HLStatsX-vX.X.X.zip` from the [Releases](https://github.com/SyntX34/hlstatsx-community-edition/releases) page.
2. Extract the archive into your CS2 server folder:
   ```text
   csgo/addons/swiftlys2/plugins/HLStatsX/
   ├── HLStatsX.dll
   ├── configs/
   │   └── HLStatsX/
   │       └── config.toml
   └── resources/
       ├── translations/
       │   ├── en.jsonc
       │   ├── da.jsonc
       │   ├── fi.jsonc
       │   ├── fr.jsonc
       │   └── hu.jsonc
       └── panorama/
           ├── layout/custom_game/
           │   ├── hlx_menu.xml
           │   ├── hlx_csay.xml
           │   └── hlx_tsay.xml
           └── styles/custom_game/
               ├── hlx_menu.css
               ├── hlx_csay.css
               └── hlx_tsay.css
   ```

### 3. Server Configuration & How Logging Works

Unlike older Source games (CS:S, CS:GO), **Counter-Strike 2 removed the native `logaddress_add` command**.

The **SwiftlyS2 HLStatsX plugin completely solves this** by hooking internal CS2 game events, weapon events, player actions, and chat, formatting them into standard HLstatsX log lines, and sending them directly via its built-in **asynchronous UDP client** to the HLstatsX:CE daemon (`DaemonHost:DaemonPort`).

In your `game/csgo/cfg/server.cfg`, you only need standard logging enabled:
```cfg
log on
```

> **Note:** Do **NOT** use `logaddress_add` in CS2. All log streaming, proxy headers (`ProxyKey`), and bi-directional command execution are handled natively over UDP by this plugin via `config.toml`.
> **Note:** ``logaddress_add`` or any method to forward logging details to external api points is removed in cs2. In cs2 the only way to forward logs is to use a plugin or server mod that hooks into the game events and sends the logs to the daemon. 

---

## ⚙️ Configuration (`config.toml`)

Edit `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml` (or `csgo/addons/swiftlys2/plugins/HLStatsX/configs/HLStatsX/config.toml`):

```toml
[HLStatsX]
# Enable verbose debug logging in server console (UDP TX/RX, menu dispatches, config load)
Debug = false

# IP/Hostname of the HLstatsX:CE perl daemon
DaemonHost = "127.0.0.1"

# Port where HLstatsX:CE daemon is listening for log packets
DaemonPort = 27500

# Public IP of this game server (must match the IP configured in HLstatsX:CE Admin backend)
ServerIp = "5.135.143.217"

# Port of this game server
ServerPort = 27015

# Proxy_Key from HLstatsX Web Admin -> Settings (leave empty only if not using Proxy_Key)
ProxyKey = ""

# Maximum players allowed on server (reported to daemon)
MaxPlayers = 32

# UDP port on which the plugin listens for daemon commands (Default: ServerPort + 1)
ReceiverPort = 27016

# Menu display type:
# "1" or "BuiltIn"   = SwiftlyS2 Built-In Center Menu (Core.Menu / IMenuService) - Recommended, works out of the box
# "2" or "CustomHud" = Custom Panorama XML HUD (requires Metamod CustomHud + Workshop 3791366113)
MenuType = "1"

# Automatically close menus after X seconds (0 = disabled)
AutoCloseMenuSeconds = 15

# Paths to custom Panorama layouts (used when MenuType = "2" / "CustomHud")
CustomMenuLayout = "panorama/layout/custom_game/hlx_menu.xml"
CustomCsayLayout = "panorama/layout/custom_game/hlx_csay.xml"
CustomTsayLayout = "panorama/layout/custom_game/hlx_tsay.xml"
```

---

## 🎮 Menu Navigation & Keybinds (`Core.Menu`)

When using the built-in SwiftlyS2 menu system (`MenuType = "1"` / `"BuiltIn"`), multi-page menus such as `!hlx`, `!top10`, `!top20`, `!next`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, and `!maps` are automatically paginated (5 items per page) and controlled using SwiftlyS2's default keys:

| Action | Default Key | Config Key in `addons/swiftlys2/configs/menus/menus.jsonc` |
| :--- | :--- | :--- |
| **Navigate Up** | `Shift` | `Buttons.NavigateUp` |
| **Navigate Down** | `F` | `Buttons.NavigateDown` |
| **Select / Open** | `E` | `Buttons.Select` |
| **Close Menu** | `Tab` | `Buttons.Close` |

---

## ⌨️ Complete In-Game Player & Admin Commands

Players can type any command in public/team chat using `!command`, `/command`, or bare `command` (e.g., `!rank`, `/rank`, or `rank`), or run them directly in the client console:

### 1. Interactive Main Menu
| Command / Aliases | Description |
| :--- | :--- |
| `!hlx`, `!menu`, `!hlstatsx`, `!hlxmenu`, `!hlx_menu`, `!statsmenu` | Opens the interactive 17-item HLstatsX Main Menu (`Rank`, `Next Players`, `Top 10`, `Top 20`, `Session Stats`, `StatsMe`, `Weapon Usage`, `Weapon Accuracy`, `Hitgroup Targets`, `Player Kills`, `Action Summary`, `Map Stats`, `Clan Rankings`, `Server Status`, `Server List`, `Toggle Ranking Display`, `Help & Commands`). |

### 2. Personal Rank, Session & Progression
| Command / Aliases | Description |
| :--- | :--- |
| `!rank`, `!skill`, `!points` | Opens a 2-section OSD menu showing your **Total** stats (rank position, points, frags/K:D, headshots, favorite weapon, activity, online time) and **Session** stats. |
| `!place` | Broadcasts your current rank position and skill points to **all players** in server chat. |
| `!next` | Opens a scrollable OSD menu showing the **next 10 players ranked directly above you** and how many points you need to overtake each one. |
| `!session` | Opens a personal OSD menu with your current map/session kills, deaths, K:D, headshots, point change,frags per minute, and rank change. |
| `!session_data` | Broadcasts your current session summary to **all players** in server chat. |
| `!kpd`, `!kdratio` | Opens your personal Total & Session kills/deaths/headshots breakdown menu. |
| `!kdeath` | Broadcasts your K:D ratio and headshot stats to **all players** in server chat. |
| `!statsme` | Opens a comprehensive personal stats OSD menu (Total + Session summary, favorite weapon kills/accuracy, and playtime). |

### 3. Leaderboards, Clans & Server Lists
| Command / Aliases | Description |
| :--- | :--- |
| `!top`, `!top5`, `!top10`, `!top12`, `!top15`, `!top20` | Opens a scrollable multi-page OSD menu listing the **Top N** players on the server (`Rank`, `Skill Points`, `Name`, and `K:D`). Bare `!top` defaults to Top 10. |
| `!clans` | Opens a scrollable OSD menu showing top-ranked clans by average member skill points, member count, kills, and K:D. |
| `!bans`, `!cheaters` | Opens a scrollable OSD menu listing banned/cheater accounts and their ban dates. |
| `!servers` | Opens a scrollable OSD menu listing all participating game servers, current maps, and live player counts. |
| `!status` | Opens a server status OSD menu showing the current server name, map, uptime, live players, and total server kills/headshots. |
| `!load` | Opens a server load OSD menu showing current slots, active players, and minimum required trackable players. |
| `!help`, `!cmd`, `!cmds`, `!commands` | Opens a scrollable in-game command reference menu. |

### 4. Weapon, Accuracy, Hitgroup, Victim, Action & Map Menus
| Command / Aliases | Description |
| :--- | :--- |
| `!weapons`, `!weapon`, `!weapon_usage` | Opens a scrollable menu of your top 10 weapons ranked by kills, % of total kills, headshots, and HS%. |
| `!accuracy` | Opens a scrollable menu of your per-weapon shots fired, hits, accuracy %, and headshot %. |
| `!targets`, `!target` | Opens a scrollable hitgroup breakdown menu (`Head`, `Chest`, `Stomach`, `Left Arm`, `Right Arm`, `Left Leg`, `Right Leg`) showing hits, hit %, and kills per body part. |
| `!kills`, `!kill`, `!player_kills` | Opens a scrollable menu of your top 10 victims (players you have killed at least 5 times), including K:D and headshots against each. |
| `!actions`, `!action` | Opens a scrollable menu of your completed objective/server actions (`Bomb Planted`, `Bomb Defused`, `Hostage Rescued`, `MVP`, etc.) and bonus points earned. |
| `!maps`, `!map`, `!map_stats` | Opens a scrollable menu of your kills, deaths, K:D, and headshots broken down by map (`de_dust2`, `de_mirage`, etc.). |
| `!<weapon_code>` | Type any CS2 weapon code (for example `!ak47`, `!awp`, `!m4a1`, `!m4a1_silencer`, `!deagle`, `!usp_silencer`, `!glock`, `!ssg08`, `!aug`, `!sg556`, `!famas`, `!galilar`, `!mp9`, `!mac10`, `!mp7`, `!ump45`, `!p90`, `!knife`, `!hegrenade`, `!inferno`, `!taser`) to open your personal stats for that specific weapon. |

### 5. Player Preferences, Auto-Commands & Admin Controls
| Command / Aliases | Description |
| :--- | :--- |
| `!hlx_hideranking` | Toggles whether your profile is hidden or visible on public leaderboards. |
| `!hlx_display 0\|1` | Enables (`1`) or disables (`0`) personal console/OSD event notifications. |
| `!hlx_chat 0\|1` | Enables (`1`) or disables (`0`) HLstatsX chat event messages for yourself. |
| `!hlx_auto start\|end\|kill <command>` | Automatically runs a stats command (`rank`, `session`, `statsme`, `top10`, etc.) on round-start (`start`), round-end (`end`), or after a kill (`kill`). |
| `!hlx_auto clear` | Clears your configured automatic command trigger. |
| `!hlx_set realname\|email\|homepage <value>` | Updates your profile metadata shown on the HLstatsX web portal. |
| `!hlx_teams 0\|1` | Enables or disables HLstatsX automatic team balancing. |

---

## 🔧 Daemon Commands (`UDP HLX_CMD` / Server Console)

The plugin listens on `ReceiverPort` (`ServerPort + 1`) and also registers these commands in the server console:

| Command | Usage | Description |
| :--- | :--- | :--- |
| `hlx_sm_msay` | `<time> <userid> [need_handler] <message>` | Displays a multi-page scrollable OSD menu (`Core.Menu` when `MenuType = "1"` or Panorama CustomHud when `MenuType = "2"`). |
| `hlx_sm_psay` | `"<userid_list>" [color] "<message>"` | Sends a color-formatted chat message to one or more players (or `"0"` for broadcast). |
| `hlx_sm_psay2` | `"<userid_list>" [color] "<message>"` | Identical to `hlx_sm_psay` (secondary message channel). |
| `hlx_sm_csay` | `"<message>"` | Displays a center-screen announcement (native CS2 center text when `MenuType = "1"`, or CustomHud banner when `MenuType = "2"`). |
| `hlx_sm_tsay` | `<time> <userid> "<message>"` | Displays a top-left HUD alert (`MenuType = "2"`) or formatted chat notice (`MenuType = "1"`). |
| `hlx_sm_hint` | `"<userid_list>" "<message>"` | Displays a center/bottom hint message to the specified player(s). |
| `hlx_sm_browse` | `"<userid>" "<url>"` | Prints the target URL in player chat (fallback for legacy Source 1 MOTD commands). |
| `hlx_sm_swap` | `"<userid>"` | Swaps a player between CT and T teams (used by daemon auto-balance). |
| `hlx_sm_redirect` | `<time> "<userid>" "<ip:port>" "<reason>"` | Notifies a player of a server redirect target in chat. |
| `hlx_sm_player_action` | `<userid> "<action>"` | Logs a custom player action to the daemon for bonus points. |
| `hlx_sm_team_action` | `"<team>" "<action>"` | Logs a custom team action (`CT` / `TERRORIST`) to the daemon. |
| `hlx_sm_world_action` | `"<action>"` | Logs a custom world/server action to the daemon. |
| `hlx_protect_address` | `"<ip:port>"` | Registers a protected log address status. |
| `hlx_block_commands` | `0\|1` | Enables (`1`) or disables (`0`) suppressing HLstatsX commands from appearing in public chat. |
| `hlx_message_prefix` | `"<prefix>"` | Sets a custom prefix prepended to HLstatsX chat messages. |
| `hlx_message_prefix_clear` | *(none)* | Resets the chat message prefix to default (`[HLstatsX]`). |
| `hlxce_webpage` | `"<url>"` | Registers the HLstatsX web portal URL from the daemon. |
| `hlxce_version` | `"<version>"` | Registers the connected HLstatsX daemon version. |

---

## 🔄 Updating an Existing HLstatsX Site with Local Customizations

If you already host a customized HLstatsX:CE website (for example with custom themes, JS tweaks, or local layout changes) and only want to apply the **Counter-Strike 2 (`cs2`), SwiftlyS2 native OSD command parity, and October 2026 PHP 8.2–8.5 security/compatibility changes** without overwriting your entire `web/` folder, inspect the exact diff in commit [`007b223`](https://github.com/SyntX34/hlstatsx-community-edition/commit/007b223ee403c738a6b18baf9ee1f63013ba5a7e#diff-0e1a79d051518c85ddb92bcceb0a68de79281ba4654ad474a41902dee3910eb0).

---

## 👤 Author & Support

- **Author**: SyntX34
- **Discord**: `nh_syntx`
- **Steam**: [SyntX34 (STEAM_0:1:610472249)](https://steamcommunity.com/id/SyntX34)
- **Workshop Item**: [HLStatsX:CE CS2 Custom HUD (3791366113)](https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113)
- **Repository**: [SyntX34/hlstatsx-community-edition](https://github.com/SyntX34/hlstatsx-community-edition)

---

## 🔌 Developer API (Contract Interface)

HLStatsX provides a standalone contract interface (`HLStatsX.Contract.dll`) allowing other SwiftlyS2 plugins (such as Zombie:Reloaded, VIP Core, custom gamemodes) to trigger points, log actions, and open stats menus without hard dependencies.

### 1. Reference the Contract
Add `HLStatsX.Contract.dll` or project reference to your plugin:
```xml
<ItemGroup>
  <ProjectReference Include="..\HLStatsX.Contract\HLStatsX.Contract.csproj" />
</ItemGroup>
```

### 2. Consume via SwiftlyS2 Interface Manager
```csharp
using HLStatsX.Contract;

public class MyPlugin : BasePlugin
{
    private IHLStatsXApi? _hlxApi;

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        _hlxApi = interfaceManager.GetSharedInterface<IHLStatsXApi>("HlStatsX.API");
        _hlxApi?.RegisterConsumer("MyPlugin");
    }

    public void OnZombieInfect(IPlayer attacker, IPlayer victim)
    {
        // Awards points defined in HLStatsX Web Panel -> Manage Actions
        _hlxApi?.TriggerPlayerAction(attacker, "zombie_infection");
        _hlxApi?.TriggerPlayerAction(victim, "got_infected");
    }
}
```

A complete working example is provided in [examples/HLStatsX.Example](examples/HLStatsX.Example/HLStatsXExample.cs).

---

## 📜 License

This project is licensed under the [MIT License](LICENSE). Anyone is free to use, modify, and contribute to this project, provided that credit is given to the original author (**SyntX34**).