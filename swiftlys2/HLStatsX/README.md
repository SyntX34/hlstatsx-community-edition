<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Counter-Strike 2 Real-Time Stats &amp; Panorama HUD Integration</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>Author:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 Profile</a>
  </p>

  <p align="center">
    <b>Languages:</b>
    🇬🇧 <a href="README.md">English</a> ·
    🇩🇪 <a href="../readme/languages/de.md">Deutsch</a> ·
    🇩🇰 <a href="../readme/languages/da.md">Dansk</a> ·
    🇪🇸 <a href="../readme/languages/es.md">Español</a> ·
    🇫🇷 <a href="../readme/languages/fr.md">Français</a> ·
    🇫🇮 <a href="../readme/languages/fi.md">Suomi</a> ·
    🇭🇺 <a href="../readme/languages/hu.md">Magyar</a> ·
    🇷🇺 <a href="../readme/languages/ru.md">Русский</a> ·
    🇨🇳 <a href="../readme/languages/chi.md">中文</a> ·
    🇯🇵 <a href="../readme/languages/ja.md">日本語</a> ·
    🇰🇷 <a href="../readme/languages/kor.md">한국어</a>
  </p>
</div>

---

## 📌 Overview

This plugin bridges **Counter-Strike 2** dedicated servers running the [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) framework with the **HLstatsX:CE** Perl backend daemon and web statistics interface.

It provides bi-directional UDP communication, full weapon/hitgroup/action tracking, live `ClanTag` rank synchronization, and **4 selectable in-game menu modes** (SwiftlyS2 Built-In, Panorama Keypress, Panorama Clickable, and Panorama Scrollable).

> 📖 **Need help setting up or debugging?** Read the **[Installation Guide](../readme/wiki/installation.md)** and **[Debug &amp; Troubleshooting](../readme/wiki/debug.md)**.

---

## ✨ Features

- **🎮 Full Stats Tracking**: Weapon kills, headshots, hits, accuracy, hitgroups, suicides, team kills, bomb plants/defuses, hostage events, MVPs, and live CS2 `ClanTag` synchronization.
- **💬 Chat Command Interception**: Intercepts `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help`, and weapon codes (`!ak47`, `!awp`, etc.) without colliding with built-in Source 2 engine commands.
- **📊 4 Selectable Menu Modes (`MenuType`)**:
  - `1` — **Built-In**: SwiftlyS2 native `Core.Menu` (no client downloads required).
  - `2` — **Keypress**: Custom Panorama HUD with `1–7` selection, `7/8` Prev/Next, `9` Exit.
  - `3` — **Clickable**: Custom Panorama HUD with full mouse cursor interaction.
  - `4` — **Scrollable**: Custom Panorama HUD with `W`/`S` navigate, `A`/`D` pages, `E` select, `R` exit.
- **🖥️ CSAY & TSAY Displays**: Center (`hlx_sm_csay`) and top-left (`hlx_sm_tsay`) HUD/chat announcements.
- **🔄 Multi-Map Continuity**: Automatically maintains UDP connectivity and re-announces players across map changes.

---

## 🖼️ Menu Previews

### 1. Built-In Menu (`MenuType = 1`)
| Rank | Top 10 | Next Players |
| :---: | :---: | :---: |
| ![Built-In Rank](../readme/built-in/rank.JPG) | ![Built-In Top10](../readme/built-in/top10.JPG) | ![Built-In Next](../readme/built-in/next.JPG) |

| Weapons | Map Stats | Servers |
| :---: | :---: | :---: |
| ![Built-In Weapons](../readme/built-in/weapons.JPG) | ![Built-In Maps](../readme/built-in/maps.JPG) | ![Built-In Servers](../readme/built-in/servers.JPG) |

### 2. Custom Panorama Keypress (`MenuType = 2`)
| Main Menu | Rank | Top 10 |
| :---: | :---: | :---: |
| ![Keypress Menu](../readme/keypress/menu.JPG) | ![Keypress Rank](../readme/keypress/rank.JPG) | ![Keypress Top10](../readme/keypress/top10.JPG) |

| Weapons | Map Stats |
| :---: | :---: |
| ![Keypress Weapons](../readme/keypress/weapons.JPG) | ![Keypress Maps](../readme/keypress/maps.JPG) |

### 3. Custom Panorama Clickable (`MenuType = 3`)
| Main Menu | Rank | Top 10 |
| :---: | :---: | :---: |
| ![Clickable Menu](../readme/clickable/menu.JPG) | ![Clickable Rank](../readme/clickable/rank.JPG) | ![Clickable Top10](../readme/clickable/top10.JPG) |

| Weapons | Map Stats |
| :---: | :---: |
| ![Clickable Weapons](../readme/clickable/weapons.JPG) | ![Clickable Maps](../readme/clickable/maps.JPG) |

### 4. Custom Panorama Scrollable (`MenuType = 4`)
| Main Menu | Rank | Top 10 |
| :---: | :---: | :---: |
| ![Scrollable Menu](../readme/scrollable/menu.JPG) | ![Scrollable Rank](../readme/scrollable/rank.JPG) | ![Scrollable Top10](../readme/scrollable/top10.JPG) |

| Next Players | Map Stats |
| :---: | :---: |
| ![Scrollable Next](../readme/scrollable/next.JPG) | ![Scrollable Maps](../readme/scrollable/maps.JPG) |

---

## 🚀 Quick Start

1. Download the latest `HLStatsX-vX.X.X.zip` from [Releases](https://github.com/SyntX34/hlstatsx-community-edition/releases) and extract it to `csgo/addons/swiftlys2/plugins/HLStatsX/`.
2. Edit `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml` (`DaemonHost`, `DaemonPort`, `ServerIp`, `ServerPort`, `ReceiverPort`, `MenuType`).
3. In HLstatsX Web Admin, set your CS2 server's **Mod** to **`SWIFTLYS2`**.
4. For `MenuType = 2, 3, or 4`, mount Steam Workshop item **`3791366113`**.

For detailed instructions, configuration reference, and troubleshooting, see the **[Installation Guide](../readme/wiki/installation.md)** and **[Debug &amp; Troubleshooting](../readme/wiki/debug.md)**.
