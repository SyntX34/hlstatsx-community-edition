<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Counter-Strike 2 Valós Idejű Statisztika &amp; Panorama HUD Integráció</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="../../HLStatsX/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>Készítő:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 Profil</a>
  </p>

  <p align="center">
    <b>Nyelvek:</b>
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

## 📌 Áttekintés

Ez a plugin összeköti a [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) alapú **Counter-Strike 2** szervereket a **HLstatsX:CE** Perl háttérszolgáltatással és webes felülettel.

> 📖 **Segítségre van szükséged a telepítéshez vagy hibakereséshez?** Nézd meg a **[Telepítési és Hibakeresési Wikit](../WIKI.md)**.

---

## ✨ Funkciók

- **🎮 Teljes statisztikakövetés**: Ölések, fejlövések, találatok, pontosság, találati zónák, bomba/túsz események, MVP-k és élő `ClanTag`.
- **💬 Chat parancsok**: `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help`.
- **📊 4 menümód (`MenuType`)**:
  - `1` — **Built-In**: SwiftlyS2 beépített `Core.Menu`.
  - `2` — **Keypress**: Egyedi Panorama HUD (`1–7` választás, `7/8` Előző/Következő, `9` Kilépés).
  - `3` — **Clickable**: Egyedi Panorama HUD egérvezérléssel.
  - `4` — **Scrollable**: Egyedi Panorama HUD (`W`/`S` navigáció, `A`/`D` lapozás, `E` választás, `R` kilépés).

---

## 🖼️ Menü Előnézetek

| Built-In (`MenuType = 1`) | Keypress (`MenuType = 2`) | Clickable (`MenuType = 3`) | Scrollable (`MenuType = 4`) |
| :---: | :---: | :---: | :---: |
| ![Built-In Rank](../built-in/rank.JPG) | ![Keypress Menu](../keypress/menu.JPG) | ![Clickable Menu](../clickable/menu.JPG) | ![Scrollable Menu](../scrollable/menu.JPG) |
