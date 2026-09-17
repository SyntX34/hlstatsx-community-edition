<div align="center">
  <h2><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h2>
  <h3>Counter-Strike 2 Plugin for HLstatsX Community Edition</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/framework-SwiftlyS2-blue" alt="SwiftlyS2">
  <img src="https://img.shields.io/badge/game-CS2-orange" alt="CS2">
  <img src="https://img.shields.io/badge/target-.NET%2010.0-purple" alt=".NET 10">
  <img src="https://img.shields.io/badge/license-GPLv2-green" alt="License">
</p>

---

## 📌 Overview

This plugin bridges **Counter-Strike 2** dedicated servers with the **HLstatsX:CE** Perl backend daemon and web frontend using the modern **SwiftlyS2** framework.

### Features
- **Chat Command Interception**: Intercepts player commands (`/rank`, `/kpd`, `/top20`, `/session`, `/next`, `/load`, `/statsme`, `/hlx`, etc.), hides them from public chat, and dispatches them to the game event log.
- **RCON Client Message Handlers**: Implements `hlx_sm_psay`, `hlx_sm_csay`, `hlx_sm_hint`, and `hlx_sm_msay` commands so backend stats and event announcements display seamlessly in player chat and HUD.
- **Address Protection**: Protects log forwarding addresses with `hlx_protect_address`.

---

## 🛠️ Installation & Setup

1. **Prerequisites**:
   - Counter-Strike 2 dedicated server
   - [MetaMod:Source](https://www.sourcemm.net/) (v2.0+)
   - [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) framework

2. **Building the Plugin**:
   ```bash
   dotnet publish -c Release
   ```
   The compiled plugin package will be in `build/publish/HLStatsX` (and zipped as `HLStatsX.zip`).

3. **Deploying**:
   Copy the `HLStatsX` folder into your CS2 server's `addons/swiftlys2/plugins/` directory:
   ```
   csgo/addons/swiftlys2/plugins/HLStatsX/
     ├── HLStatsX.dll
     └── resources/
   ```

4. **Server Configuration**:
   Add the following to your `server.cfg`:
   ```cfg
   log on
   logaddress_add <DAEMON_IP>:<PORT>
   ```

---

## ⚙️ Commands

| Command | Type | Description |
| :--- | :--- | :--- |
| `hlx_sm_psay <userid> <color> <msg>` | Server | Sends formatted private or broadcast chat message |
| `hlx_sm_csay <msg>` | Server | Displays center alert message |
| `hlx_sm_hint <userid> <msg>` | Server | Displays HUD hint message |
| `hlx_sm_msay <userid> <time> <msg>` | Server | Multiline chat/menu message display |
| `hlx_protect_address <ip:port>` | Server | Configures and protects backend log ingestion address |