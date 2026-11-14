<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Counter-Strike 2 Realtidsstatistik &amp; Panorama HUD-integration</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="../../HLStatsX/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>Forfatter:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 Profil</a>
  </p>

  <p align="center">
    <b>Sprog:</b>
    🇬🇧 <a href="../../HLStatsX/README.md">English</a> ·
    🇩🇪 <a href="de.md">Deutsch</a> ·
    🇩🇰 <a href="da.md">Dansk</a> ·
    🇪🇸 <a href="es.md">Español</a> ·
    🇫🇷 <a href="fr.md">Français</a> ·
    🇫🇮 <a href="fi.md">Suomi</a> ·
    🇭🇺 <a href="hu.md">Magyar</a> ·
    🇷🇺 <a href="ru.md">Русский</a> ·
    🇨🇳 <a href="chi.md">中文</a> ·
    🇯🇵 <a href="ja.md">日本語</a> ·
    🇰🇷 <a href="kor.md">한국어</a>
  </p>
</div>

---

## 📌 Oversigt

Dette plugin forbinder **Counter-Strike 2** dedikerede servere med [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) til **HLstatsX:CE** Perl-daemonen og webstatistikken.

Det understøtter tovejs UDP-kommunikation, fuld statistiksporing, live `ClanTag`-synkronisering og **4 valgfri menutilstande** (SwiftlyS2 Built-In, Panorama Keypress, Panorama Clickable og Panorama Scrollable).

> 📖 **Brug for hjælp til opsætning eller fejlfinding?** Se **[Setup &amp; Debug Wiki](../WIKI.md)**.

---

## ✨ Funktioner

- **🎮 Fuld statistiksporing**: Drab, headshots, træffere, præcision, hitgroups, selvmord, teamkills, bombe-/gidsel-events, MVP'er og live `ClanTag`.
- **💬 Chat-kommandoer**: Opfanger `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help` m.fl.
- **📊 4 menutilstande (`MenuType`)**:
  - `1` — **Built-In**: SwiftlyS2 indbygget `Core.Menu` (ingen klientdownloads krævet).
  - `2` — **Keypress**: Brugerdefineret Panorama HUD med `1–7` valg, `7/8` Forrige/Næste, `9` Luk.
  - `3` — **Clickable**: Brugerdefineret Panorama HUD med fuld musestyring.
  - `4` — **Scrollable**: Brugerdefineret Panorama HUD med `W`/`S` navigation, `A`/`D` sider, `E` vælg, `R` luk.
- **🖥️ CSAY &amp; TSAY**: Center- (`hlx_sm_csay`) og øverst-venstre (`hlx_sm_tsay`) beskeder.

---

## 🖼️ Menuforhåndsvisning

| Built-In (`MenuType = 1`) | Keypress (`MenuType = 2`) | Clickable (`MenuType = 3`) | Scrollable (`MenuType = 4`) |
| :---: | :---: | :---: | :---: |
| ![Built-In Rank](../built-in/rank.JPG) | ![Keypress Menu](../keypress/menu.JPG) | ![Clickable Menu](../clickable/menu.JPG) | ![Scrollable Menu](../scrollable/menu.JPG) |

---

## 🚀 Hurtig start

1. Download den nyeste `HLStatsX-vX.X.X.zip` og udpak til `csgo/addons/swiftlys2/plugins/HLStatsX/`.
2. Rediger `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml`.
3. I HLstatsX Web Admin sættes serverens **Mod** til **`SWIFTLYS2`**.
4. For `MenuType = 2, 3 eller 4` tilføjes Steam Workshop **`3791366113`**.
