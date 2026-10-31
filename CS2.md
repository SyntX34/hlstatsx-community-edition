# HLstatsX:CE — Counter-Strike 2 (SwiftlyS2) Setup, Configuration & Debugging Guide

This guide covers end-to-end installation, configuration, `Proxy_Key` setup, bidirectional UDP communication, `systemd` service setup, and packet/log debugging (`tcpdump`, `journalctl`, Windows PowerShell/Wireshark) for **Counter-Strike 2** using the **SwiftlyS2** HLStatsX plugin on both **Linux** and **Windows**.

---

## 1. Architecture Overview (How CS2 Talks to HLstatsX:CE)

Unlike legacy Source 1 (`logaddress_add` + TCP RCON), the CS2 SwiftlyS2 integration uses a fast, non-blocking **bidirectional UDP** architecture:

```
┌────────────────────────────────────────┐          UDP Log Stream (Port 27500)          ┌────────────────────────────────────────┐
│      Counter-Strike 2 Game Server      │ ────────────────────────────────────────────► │         HLstatsX:CE Perl Daemon        │
│     SwiftlyS2 + HLStatsX Plugin        │   (Optional: PROXY Key=... ServerIP:Port)     │             (hlstats.pl)               │
│                                        │                                               │                                        │
│  Game Port:     27015                  │ ◄──────────────────────────────────────────── │  Mod Type:      SWIFTLYS2              │
│  ReceiverPort:  27016 (ServerPort + 1) │       UDP Commands: HLX_CMD hlx_sm_...        │  Target Port:   ServerPort + 1 (27016) │
└────────────────────────────────────────┘                                               └────────────────────────────────────────┘
```

1. **CS2 Plugin -> Perl Daemon (`DaemonHost:DaemonPort`, default `27500/udp`)**:
   - The SwiftlyS2 plugin formats all player connects, disconnects, kills, headshots, weapon/hitgroup stats (`weaponstats` / `weaponstats2`), bomb/hostage actions, map transitions, and chat commands (`rank`, `top10`, `statsme`, `session`, etc.) as standard Source log lines and sends them via UDP to `hlstats.pl`.
   - When `ProxyKey` is set in `config.toml`, each packet is prefixed with `PROXY Key=<ProxyKey> <ServerIp>:<ServerPort>PROXY L ...` so the daemon attributes logs to your configured `ServerIp:ServerPort` even if your CS2 server is behind NAT, Docker, Pterodactyl, or on the same machine (`127.0.0.1`).
2. **Perl Daemon -> CS2 Plugin (`ServerIp:ReceiverPort`, default `27016/udp`)**:
   - When the server's **Mod** is set to **`SWIFTLYS2`** in the HLstatsX Web Admin panel, the daemon sends in-game responses (`hlx_sm_psay`, `hlx_sm_msay`, `hlx_sm_csay`, `hlx_sm_tsay`, `hlx_sm_hint`, etc.) prefixed with `HLX_CMD ` via UDP to `ReceiverPort` (`ServerPort + 1` by default, or custom `PluginPort`).

---

## 2. Web Panel & Database Setup

### Step 2.1: Run the Database Updater
1. Ensure your web server runs one of the patched PHP versions:
   - **PHP 8.5.12+**
   - **PHP 8.4.27+**
   - **PHP 8.3.36+**
   - **PHP 8.2.35+**
2. Open `https://your-domain.com/updater` (or `hlstats.php?mode=updater`) to apply migrations up to **DB Version 84 (`web/updater/84.php`)**, which includes the `cs2` game profile and weapon/action definitions.

### Step 2.2: How to Get or Set Your `Proxy_Key`
The `Proxy_Key` allows the SwiftlyS2 plugin to authenticate its UDP packets and explicitly tell the daemon which `ServerIp:ServerPort` the logs belong to. **This is strongly recommended** (especially on Pterodactyl, Docker, NAT, or multi-IP hosts).

1. Log in to your **HLstatsX Web Admin** (`hlstats.php?mode=admin`).
2. Navigate to **General Settings -> HLstatsX:CE Settings**.
3. Scroll to **`Proxy_Key`** (*"Key to use when receiving data from proxy-daemon.pl"*).
4. Set a strong secret string (for example: `MySecretHlxKey2026`) and click **Apply**.
   - Alternatively, you can check or set it directly in MySQL:
     ```sql
     SELECT keyname, value FROM hlstats_Options WHERE keyname = 'Proxy_Key';
     UPDATE hlstats_Options SET value = 'MySecretHlxKey2026' WHERE keyname = 'Proxy_Key';
     ```
5. Restart `hlstats.pl` after changing `Proxy_Key` so the daemon reloads the setting from the database.

### Step 2.3: Add Your CS2 Server in Web Admin
1. In **HLstatsX Web Admin**, go to **Games -> Counter-Strike 2 (`cs2`) -> Edit Servers**.
2. Add your server with:
   - **IP Address**: Your CS2 server's public IP (e.g., `203.0.113.50`) — must match `ServerIp` in `config.toml`.
   - **Port**: Your CS2 game port (e.g., `27015`) — must match `ServerPort` in `config.toml`.
   - **Server Name**: Your server title.
   - **RCON Password**: Can be left blank or any string (SwiftlyS2 uses UDP `HLX_CMD` instead of TCP RCON).
3. Click **Configure** on the newly added server and verify these critical settings:
   - **Mod**: **`SWIFTLYS2`** (Required so the daemon sends `HLX_CMD` UDP packets and `hlx_sm_msay` menus!).
   - **BroadCastEvents**: `1` (Enabled)
   - **BroadCastPlayerActions**: `1` (Enabled)
   - **BroadCastEventsCommand**: `hlx_sm_psay`
   - **BroadCastEventsCommandAnnounce**: `hlx_sm_csay`
   - **PlayerEvents**: `1` (Enabled)
   - **PlayerEventsCommand**: `hlx_sm_psay`
   - **PlayerEventsCommandOSD**: `hlx_sm_msay` (Required for `!rank`, `!top10`, `!statsme`, `!session` menus!)
   - **PlayerEventsCommandHint**: `hlx_sm_hint`
   - **MinPlayers**: Set to `1` or `2` while testing so kills/actions are recorded immediately even with 1–2 players on the server.
4. Save changes and reload/restart the Perl daemon.

---

## 3. Building & Installing the SwiftlyS2 Plugin (Linux & Windows)

### Step 3.1: Build from Source (.NET 10 SDK)
On either Linux or Windows:
```bash
dotnet publish swiftlys2/HLStatsX/HLStatsX.csproj -c Release
```
This compiles `HLStatsX.Contract.dll` and `HLStatsX.dll` and creates a ready-to-extract package at:
- Folder: `swiftlys2/HLStatsX/build/publish/HLStatsX/`
- Archive: `swiftlys2/HLStatsX/build/HLStatsX.zip`

### Step 3.2: Install onto Your CS2 Server
Copy the built files into your CS2 server's `game/csgo/addons/swiftlys2/` directory:

#### Linux CS2 Server Paths
```text
game/csgo/addons/swiftlys2/
├── plugins/
│   └── HLStatsX/
│       ├── HLStatsX.dll
│       ├── HLStatsX.Contract.dll
│       ├── configs/
│       │   └── HLStatsX/
│       │       └── config.toml
│       └── resources/
│           ├── gamedata/
│           ├── panorama/
│           │   └── layout/custom_game/
│           │       ├── hlx_menu.xml
│           │       ├── hlx_csay.xml
│           │       └── hlx_tsay.xml
│           └── translations/
└── configs/
    └── plugins/
        └── HLStatsX/
            └── config.toml
```

#### Windows CS2 Server Paths
```text
game\csgo\addons\swiftlys2\plugins\HLStatsX\HLStatsX.dll
game\csgo\addons\swiftlys2\configs\plugins\HLStatsX\config.toml
```
*(Note: The plugin automatically checks both `addons/swiftlys2/configs/plugins/HLStatsX/config.toml` and `plugins/HLStatsX/configs/HLStatsX/config.toml`.)*

---

## 4. Configuring `config.toml`

Edit `addons/swiftlys2/configs/plugins/HLStatsX/config.toml`:

```toml
[HLStatsX]
# Enable verbose debug logging in server console (UDP TX/RX, menu dispatches, config load)
Debug = false

# IP address or hostname where your hlstats.pl daemon is listening
DaemonHost = "127.0.0.1"

# UDP port where your hlstats.pl daemon is listening
DaemonPort = 27500

# Public IP of this CS2 server (MUST match the IP configured in HLstatsX Web Admin -> Servers)
ServerIp = "203.0.113.50"

# Game port of this CS2 server (MUST match the Port configured in HLstatsX Web Admin -> Servers)
ServerPort = 27015

# Proxy_Key from HLstatsX Web Admin -> Settings (leave empty only if not using Proxy_Key)
ProxyKey = "MySecretHlxKey2026"

# Maximum player slots reported to HLstatsX daemon on map start
MaxPlayers = 32

# UDP port where this plugin listens for HLX_CMD commands from the Perl daemon
# IMPORTANT: Default is ServerPort + 1 (e.g. 27016 when ServerPort = 27015)
ReceiverPort = 27016

# Menu rendering style for !hlx / !menu / !rank / !top10 / !statsme:
# "1" or "BuiltIn"   = SwiftlyS2 Built-In Center Menu (Core.Menu / IMenuService) - Recommended, works out of the box
# "2" or "CustomHud" = Custom Panorama XML HUD (requires Metamod CustomHud addon + resource precache)
MenuType = "1"

# Seconds before an open HLstatsX menu automatically closes (0 = disable auto-close)
AutoCloseMenuSeconds = 15

# Panorama layout paths when MenuType = "CustomHud" (must use "panorama/layout/..." relative to package root)
CustomMenuLayout = "panorama/layout/custom_game/hlx_menu.xml"
CustomCsayLayout = "panorama/layout/custom_game/hlx_csay.xml"
CustomTsayLayout = "panorama/layout/custom_game/hlx_tsay.xml"
```

> **Important Note on `ReceiverPort` & Firewalls**:
> By default, `hlstats.pl` sends commands back to `<ServerIp>:<ServerPort + 1>` (e.g. `27016/udp`).
> Make sure UDP port `27016` is open in your firewall and mapped in Docker/Pterodactyl if your CS2 server runs in a container!
> If you want to use a different UDP port than `ServerPort + 1`, insert a `PluginPort` parameter for that server in `hlstats_Servers_Config`:
> ```sql
> INSERT INTO hlstats_Servers_Config (serverId, parameter, value) VALUES (1, 'PluginPort', '27016');
> ```

---

## 5. Running the Perl Daemon (`hlstats.pl`)

### Option A: Linux `systemd` Service (Recommended)

1. Create `/etc/systemd/system/hlstatsx.service`:
   ```ini
   [Unit]
   Description=HLstatsX:CE Real-Time Log Daemon
   After=network.target mysql.service mariadb.service
   Wants=mysql.service

   [Service]
   Type=simple
   User=hlstats
   Group=hlstats
   WorkingDirectory=/opt/hlstatsx/scripts
   ExecStart=/usr/bin/perl /opt/hlstatsx/scripts/hlstats.pl --configfile=/opt/hlstatsx/scripts/hlstats.conf
   ExecReload=/bin/kill -HUP $MAINPID
   Restart=always
   RestartSec=3
   StandardOutput=journal
   StandardError=journal
   SyslogIdentifier=hlstatsx

   [Install]
   WantedBy=multi-user.target
   ```

2. Enable and start the service:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable --now hlstatsx
   ```

3. Watch live daemon logs in `journalctl`:
   ```bash
   sudo journalctl -u hlstatsx -f
   ```
   You will see structured real-time logs for:
   - Server initialization (`S_CONFIG`, `SERVER`)
   - Player connections, validations, team joins, chat commands (`E001`, `E002`, `E014`, `E019`)
   - Outbound UDP commands to the SwiftlyS2 plugin (`SWS2_TX   : -> 203.0.113.50:27016 : hlx_sm_msay "15" "1" ...`)
   - Database flushes (`MYSQL     : Flushed 4 player update(s) to database`)

### Option B: Windows (PowerShell / NSSM / Task Scheduler)

1. Install **Strawberry Perl** (`https://strawberryperl.com/`) and required DBD::mysql / GeoIP2 modules:
   ```powershell
   cpanm DBI DBD::mysql ConfigReader::Simple
   ```
2. Run interactively in PowerShell to test:
   ```powershell
   cd D:\hlstatsx\hlstatsx-community-edition\scripts
   perl .\hlstats.pl --configfile=.\hlstats.conf --debug
   ```
3. To run as a background Windows Service, use **NSSM (Non-Sucking Service Manager)**:
   ```powershell
   nssm install HLstatsX "C:\Strawberry\perl\bin\perl.exe" "D:\hlstatsx\hlstatsx-community-edition\scripts\hlstats.pl --configfile=D:\hlstatsx\hlstatsx-community-edition\scripts\hlstats.conf"
   nssm set HLstatsX AppDirectory "D:\hlstatsx\hlstatsx-community-edition\scripts"
   nssm start HLstatsX
   ```

---

## 6. Debugging & Packet Verification Guide (Linux & Windows)

If stats are not recording or in-game menus (`!hlx`, `!rank`, `!top10`) are not appearing, follow this step-by-step checklist.

### Step 6.1: Turn On Debug Mode in `config.toml`
Set `Debug = true` in `addons/swiftlys2/configs/plugins/HLStatsX/config.toml` and reload the plugin or change map.
In the CS2 server console, you will see:
```text
[HLstatsX:CE] Loaded. Sending logs to 127.0.0.1:27500 (Server: 203.0.113.50:27015, ReceiverPort: 27016, MaxPlayers: 32, Debug: True)
[HLstatsX:CE] Listening for daemon commands on UDP port 27016
[HLstatsX:CE DEBUG] UDP TX to 127.0.0.1:27500: "Player<1><[U:1:123456]><CT>" say "rank"
[HLstatsX:CE DEBUG] UDP RX from 127.0.0.1:54321: HLX_CMD hlx_sm_msay "15" "1" "->1 - Total\n   Position 1 of 10..."
[HLstatsX:CE DEBUG] Displaying stats menu for Player (UserID=1, PlayerID=0, Duration=15s)
```
- **If you see `UDP TX` but no `UDP RX`**:
  1. Check `journalctl -u hlstatsx -f` to see if the daemon received the packet.
  2. If `journalctl` says `NOT ALLOWED SERVER` or `proxy_key mismatch`:
     - Verify `ServerIp` and `ServerPort` in `config.toml` exactly match the IP and Port in **HLstatsX Web Admin -> Servers**.
     - Verify `ProxyKey` in `config.toml` matches `Proxy_Key` in **HLstatsX Web Admin -> Settings**.
  3. If `journalctl` shows `SWS2_TX : -> <ip>:<port> : hlx_sm_msay ...`, then the daemon **did** send the menu back, and your firewall/NAT/Pterodactyl port allocation is blocking UDP port `27016` (`ReceiverPort`) from reaching the CS2 server.

### Step 6.2: Using `tcpdump` on Linux to Inspect UDP Traffic
Use `tcpdump` on Linux to confirm packets are flowing in both directions (`-A` prints ASCII payload, `-n` skips DNS lookups, `-i any` captures both loopback `lo` and external interfaces):

1. **Verify CS2 -> Daemon UDP packets (Port `27500`)**:
   ```bash
   sudo tcpdump -i any -n -A udp port 27500
   ```
   Expected output when a player types `!rank` or gets a kill:
   ```text
   IP 203.0.113.50.49152 > 203.0.113.50.27500: UDP, length 118
   E..z..@.@..!...2...2...l..f.PROXY Key=MySecretHlxKey2026 203.0.113.50:27015PROXY L 10/24/2026 - 09:30:00: "SyntX<1><[U:1:123456]><CT>" say "rank"
   ```

2. **Verify Daemon -> CS2 UDP command packets (Port `27016` / `ReceiverPort`)**:
   ```bash
   sudo tcpdump -i any -n -A udp port 27016
   ```
   Expected output when the daemon replies with a menu or chat message:
   ```text
   IP 203.0.113.50.55123 > 203.0.113.50.27016: UDP, length 184
   E.....@.@......2...2...p....HLX_CMD hlx_sm_msay "15" "1" "->1 - Total\n   Position 1 of 25\n   1,042 Points\n   15:4 Frags (3.75)\n..."
   ```

3. **Send a Manual Test UDP Command on Linux (`nc` / `netcat`)**:
   You can test the CS2 plugin's UDP listener directly from a Linux terminal without the daemon:
   ```bash
   echo -n 'HLX_CMD hlx_sm_psay "0" 1 "[HLstatsX] Test UDP broadcast working!"' | nc -u -w1 127.0.0.1 27016
   ```

### Step 6.3: Debugging UDP Traffic on Windows (PowerShell & Wireshark)

1. **Check that CS2 (`ReceiverPort` 27016) and Perl (`DaemonPort` 27500) are bound**:
   ```powershell
   Get-NetUDPEndpoint -LocalPort 27500, 27016 -ErrorAction SilentlyContinue | Format-Table LocalAddress, LocalPort, OwningProcess
   ```

2. **Send a Manual Test UDP Command from PowerShell to CS2 (`ReceiverPort` 27016)**:
   ```powershell
   $udp = New-Object System.Net.Sockets.UdpClient
   $bytes = [System.Text.Encoding]::UTF8.GetBytes('HLX_CMD hlx_sm_psay "0" 1 "[HLstatsX] Windows UDP test successful!"')
   $udp.Send($bytes, $bytes.Length, "127.0.0.1", 27016)
   $udp.Close()
   ```

3. **Inspect Live Packets in Wireshark (Windows)**:
   - Select the **Adapter for loopback traffic capture** (if local) or your Ethernet adapter.
   - Apply display filter:
     ```text
     udp.port == 27500 || udp.port == 27016
     ```

### Step 6.4: Common Pitfalls & Quick Fixes

| Symptom | Cause | Solution |
|---------|-------|----------|
| `(IGNORED) NOTMINPLAYERS` in daemon logs | Server has fewer players than `MinPlayers` (default `4` or `6`). | Lower `MinPlayers` to `1` or `2` in **Web Admin -> Servers -> Configure**, then reload daemon. |
| `(IGNORED) NOPLAYERINFO` in daemon logs | Player was already on the server before daemon started or after map change. | Fixed in HLStatsX SwiftlyS2 v1.2.0+: plugin automatically re-announces all connected players on map load, round start, and spawn. |
| Backend shows `SWS2_TX ... hlx_sm_msay`, but no menu opens in-game | 1) `ReceiverPort` (`27016/udp`) blocked or not mapped to container.<br>2) `MenuType = "2"` (`CustomHud`) set without Metamod CustomHud installed. | 1) Open `27016/udp` and verify `ReceiverPort = 27016` in `config.toml`.<br>2) Set `MenuType = "1"` (`BuiltIn`) in `config.toml` unless Metamod CustomHud is installed and precached. |
| CustomHud layout fails validation | Layout path had `file://{resources}/` or `resources/` prefix, or XML had `html="true"`. | Use `panorama/layout/custom_game/hlx_menu.xml` (automatically normalized in v1.2.0+). |

---

## 7. Complete In-Game Commands & Menu Navigation Reference

See [swiftlys2/HLStatsX/README.md](swiftlys2/HLStatsX/README.md) for the full plugin reference, developer contract API (`HLStatsX.Contract.dll`), and command tables.

### Menu Navigation (`Core.Menu` / `MenuType = "1"`)
- **Navigate Up**: `Shift`
- **Navigate Down**: `F`
- **Select / Execute**: `E`
- **Close Menu**: `Tab`
*(Keybinds can be customized in `addons/swiftlys2/configs/menus/menus.jsonc`.)*

### Player Chat & Console Commands (`!cmd`, `/cmd`, or `cmd`)
- **Interactive Menu**: `!hlx`, `!menu`, `!hlstatsx`, `!hlxmenu`, `!hlx_menu`, `!statsmenu`
- **Personal Rank & Progression**: `!rank`, `!skill`, `!points`, `!place` (broadcast), `!next`, `!session`, `!session_data` (broadcast), `!kpd`, `!kdratio`, `!kdeath` (broadcast), `!statsme`
- **Leaderboards & Clans**: `!top`, `!top5`, `!top10`, `!top12`, `!top15`, `!top20`, `!clans`, `!bans`, `!cheaters`
- **Weapons, Hitgroups, Victims, Actions & Maps**: `!weapons` (`!weapon`, `!weapon_usage`), `!accuracy`, `!targets` (`!target`), `!kills` (`!kill`, `!player_kills`), `!actions` (`!action`), `!maps` (`!map`, `!map_stats`), and per-weapon triggers (`!ak47`, `!awp`, `!m4a1_silencer`, `!deagle`, `!usp_silencer`, `!glock`, `!ssg08`, `!knife`, `!hegrenade`, etc.)
- **Server Info & Help**: `!servers`, `!status`, `!load`, `!help`, `!cmd`, `!cmds`, `!commands`
- **Preferences & Auto-Commands**: `!hlx_hideranking`, `!hlx_display 0|1`, `!hlx_chat 0|1`, `!hlx_auto start|end|kill <cmd>`, `!hlx_auto clear`, `!hlx_set realname|email|homepage <value>`, `!hlx_teams 0|1`

---

## 8. Updating an Existing HLstatsX Site with Local Customizations

If you have local modifications on your HLstatsX web hosting (such as custom themes or frontend changes) and want to cherry-pick or inspect the exact PHP 8.2–8.5 security patch, CS2 support, and daemon changes, check commit [`007b223`](https://github.com/SyntX34/hlstatsx-community-edition/commit/007b223ee403c738a6b18baf9ee1f63013ba5a7e#diff-0e1a79d051518c85ddb92bcceb0a68de79281ba4654ad474a41902dee3910eb0).
