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

- **🎮 Full Stats Tracking**: Weapon kills, headshots, hits, accuracy, hitgroups (head, chest, stomach, arms, legs), suicides, team kills, bomb plants/defuses, hostage events, and MVPs.
- **💬 Chat Command Interception**: Intercepts player commands (`rank`, `top10`, `top20`, `next`, `session`, `statsme`, `weapons`, etc.), forwards them silently to the daemon, and suppresses spam in public chat.
- **📊 Panorama Custom HUD Menus**: Supports rich interactive Panorama HUD menus with key and mouse navigation for ranks, session stats, top 10, weapon breakdowns, and more.
- **🖥️ CSAY & TSAY Panorama Displays**:
  - `hlx_sm_csay`: Custom center HUD banner (e.g. *“Tracking X players with Y kills and Z headshots”*).
  - `hlx_sm_tsay`: Custom top-left HUD stats panel.
- **🔔 Connect & Event Announcements**: Automatically announces player connections with rank, points, and country geolocation.
- **🌐 In-Game Webpage Support**: Opens player profile or stats website directly through client commands (`hlxce_webpage`).
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
# IP/Hostname of the HLstatsX:CE perl daemon
DaemonHost = "127.0.0.1"

# Port where HLstatsX:CE daemon is listening for log packets
DaemonPort = 27500

# Public IP of this game server (must match the IP configured in HLstatsX:CE Admin backend)
ServerIp = "5.135.143.217"

# Port of this game server
ServerPort = 27015

# Optional proxy key if you use hlstatsx proxy/relay
ProxyKey = ""

# Maximum players allowed on server (reported to daemon)
MaxPlayers = 32

# UDP port on which the plugin listens for daemon commands (Default: ServerPort + 1)
ReceiverPort = 27016

# Menu display type: "CustomHud" (Panorama XML) or "BuiltIn" (Swiftly Menu API)
MenuType = "CustomHud"

# Paths to custom Panorama layouts
CustomMenuLayout = "resources/panorama/layout/custom_game/hlx_menu.xml"
CustomCsayLayout = "resources/panorama/layout/custom_game/hlx_csay.xml"
CustomTsayLayout = "resources/panorama/layout/custom_game/hlx_tsay.xml"

# Automatically close custom HUD menus after X seconds (0 = disabled)
AutoCloseMenuSeconds = 15
```

---

## ⌨️ Player Chat & Console Commands

Players can type either with `!` or `/` (or directly in console):

| Chat Command | Description |
| :--- | :--- |
| `!rank` / `rank` | Displays total stats and current session stats |
| `!top10` / `top10` | Displays top 10 ranked players on the server |
| `!top20` / `top20` | Displays top 20 ranked players on the server |
| `!next` / `next` | Shows players ranked directly above you |
| `!session` / `session` | Shows kills, deaths, headshots, and points in your current session |
| `!statsme` / `statsme` | Displays full personal weapon and hitgroup statistics |
| `!weapons` / `weapons` | Displays detailed weapon kill and accuracy breakdown |
| `!menu` / `!hlx` | Opens the interactive statistics menu |
| `!servers` | Lists other servers connected to the HLstatsX network |

---

## 🔧 Daemon Commands (UDP HLX_CMD)

The plugin listens on `ReceiverPort` (`ServerPort + 1`) and executes:

| Command | Usage | Description |
| :--- | :--- | :--- |
| `hlx_sm_msay` | `<time> <userid> [need_handler] <message>` | Displays multi-line stats breakdown in chat or HUD menu |
| `hlx_sm_psay` | `<userid> <color> <message>` | Sends formatted private or broadcast chat message |
| `hlx_sm_csay` | `<message>` | Displays center screen HUD announcement |
| `hlx_sm_tsay` | `<time> <userid> <message>` | Displays top-left screen HUD alert |
| `hlx_sm_hint` | `<userid> <message>` | Displays bottom hint alert |
| `hlxce_webpage`| `<url>` | Sets configured web stats URL |
| `hlxce_version`| `<version>` | Registers backend daemon version |

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