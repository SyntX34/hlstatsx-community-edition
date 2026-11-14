<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Counter-Strike 2 实时统计与 Panorama HUD 集成</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="../../HLStatsX/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>作者:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 主页</a>
  </p>

  <p align="center">
    <b>语言:</b>
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

## 📌 简介

该插件将基于 [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) 的 **Counter-Strike 2** 专用服务器连接到 **HLstatsX:CE** Perl 后台守护程序和网页统计系统。

> 📖 **需要安装或调试帮助？** 请查看 **[安装与调试 Wiki](../WIKI.md)**。

## ✨ 功能特性

- **🎮 完整数据统计**: 击杀、爆头、命中、准确度、命中部位、炸弹/人质事件、MVP 以及实时 `ClanTag` 同步。
- **💬 聊天指令拦截**: `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help`。
- **📊 4 种菜单模式 (`MenuType`)**:
  - `1`: **Built-In** (SwiftlyS2 原生 `Core.Menu`，无需客户端下载)。
  - `2`: **Keypress** (自定义 Panorama HUD：`1–7` 选择，`7/8` 上一页/下一页，`9` 退出)。
  - `3`: **Clickable** (自定义 Panorama HUD，支持完整鼠标点击)。
  - `4`: **Scrollable** (自定义 Panorama HUD：`W`/`S` 上下移动，`A`/`D` 翻页，`E` 选择，`R` 退出)。

## 🖼️ 菜单预览

| Built-In (`MenuType = 1`) | Keypress (`MenuType = 2`) | Clickable (`MenuType = 3`) | Scrollable (`MenuType = 4`) |
| :---: | :---: | :---: | :---: |
| ![Built-In Rank](../built-in/rank.JPG) | ![Keypress Menu](../keypress/menu.JPG) | ![Clickable Menu](../clickable/menu.JPG) | ![Scrollable Menu](../scrollable/menu.JPG) |
