<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Estadísticas en Tiempo Real para Counter-Strike 2 e Integración Panorama HUD</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="../../HLStatsX/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>Autor:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">Perfil SyntX34</a>
  </p>

  <p align="center">
    <b>Idiomas:</b>
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

## 📌 Descripción General

Este plugin conecta servidores dedicados de **Counter-Strike 2** con [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) al demonio Perl de **HLstatsX:CE** y su interfaz web.

Incluye comunicación UDP bidireccional, seguimiento completo de estadísticas, sincronización en vivo de `ClanTag` y **4 modos de menú** (SwiftlyS2 Built-In, Panorama Keypress, Panorama Clickable y Panorama Scrollable).

> 📖 **¿Necesitas ayuda para configurar o depurar?** Consulta la **[Wiki de Configuración y Depuración](../WIKI.md)**.

---

## ✨ Características

- **🎮 Seguimiento Completo**: Bajas, headshots, impactos, precisión, zonas de impacto, suicidios, bajas de equipo, eventos de bomba/rehenes, MVPs y `ClanTag` en vivo.
- **💬 Comandos de Chat**: Intercepta `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help`, etc.
- **📊 4 Modos de Menú (`MenuType`)**:
  - `1` — **Built-In**: Menú nativo `Core.Menu` de SwiftlyS2 (sin descargas).
  - `2` — **Keypress**: Panorama HUD personalizado (`1–7` seleccionar, `7/8` Anterior/Siguiente, `9` Salir).
  - `3` — **Clickable**: Panorama HUD personalizado con soporte completo de ratón.
  - `4` — **Scrollable**: Panorama HUD personalizado (`W`/`S` navegar, `A`/`D` páginas, `E` seleccionar, `R` salir).

---

## 🖼️ Vistas Previas

| Built-In (`MenuType = 1`) | Keypress (`MenuType = 2`) | Clickable (`MenuType = 3`) | Scrollable (`MenuType = 4`) |
| :---: | :---: | :---: | :---: |
| ![Built-In Rank](../built-in/rank.JPG) | ![Keypress Menu](../keypress/menu.JPG) | ![Clickable Menu](../clickable/menu.JPG) | ![Scrollable Menu](../scrollable/menu.JPG) |

---

## 🚀 Inicio Rápido

1. Descarga `HLStatsX-vX.X.X.zip` y extráelo en `csgo/addons/swiftlys2/plugins/HLStatsX/`.
2. Edita `csgo/addons/swiftlys2/configs/plugins/HLStatsX/config.toml`.
3. En HLstatsX Web Admin, establece el **Mod** del servidor en **`SWIFTLYS2`**.
4. Para `MenuType = 2, 3 o 4`, monta el elemento del Workshop **`3791366113`**.
