<div align="center">
  <h1><strong>HLStatsX:CE Ingame Plugin (SwiftlyS2)</strong></h1>
  <h3>Counter-Strike 2 실시간 통계 및 Panorama HUD 통합</h3>

  <p align="center">
    <a href="https://github.com/swiftly-solution/swiftlys2"><img src="https://img.shields.io/badge/framework-SwiftlyS2-blue?style=for-the-badge" alt="SwiftlyS2"></a>
    <img src="https://img.shields.io/badge/game-CS2-orange?style=for-the-badge" alt="CS2">
    <img src="https://img.shields.io/badge/target-.NET%2010.0-purple?style=for-the-badge" alt=".NET 10">
    <img src="https://img.shields.io/badge/version-1.3.0-red?style=for-the-badge" alt="Version 1.3.0">
    <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3791366113"><img src="https://img.shields.io/badge/Workshop-3791366113-black?style=for-the-badge&logo=steam" alt="Steam Workshop"></a>
    <a href="../../HLStatsX/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"></a>
  </p>

  <p align="center">
    <b>제작자:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34</a> |
    <b>Discord:</b> <code>nh_syntx</code> |
    <b>Steam:</b> <a href="https://steamcommunity.com/id/SyntX34">SyntX34 프로필</a>
  </p>

  <p align="center">
    <b>언어:</b>
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

## 📌 개요

이 플러그인은 [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) 기반 **Counter-Strike 2** 전용 서버를 **HLstatsX:CE** Perl 데몬 및 웹 통계와 연결합니다.

> 📖 **설치 및 디버깅 도움이 필요하신가요?** **[설치 및 디버그 위키](../WIKI.md)**를 확인하세요.

## ✨ 주요 기능

- **🎮 전체 통계 추적**: 킬, 헤드샷, 명중, 정확도, 피격 부위, 폭탄/인질 이벤트, MVP 및 실시간 `ClanTag` 동기화.
- **💬 채팅 명령어**: `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!mapstats`, `!clans`, `!servers`, `!status`, `!help`.
- **📊 4가지 메뉴 모드 (`MenuType`)**:
  - `1`: **Built-In** (SwiftlyS2 기본 `Core.Menu`, 클라이언트 다운로드 불필요).
  - `2`: **Keypress** (커스텀 Panorama HUD: `1–7` 선택, `7/8` 이전/다음, `9` 닫기).
  - `3`: **Clickable** (커스텀 Panorama HUD: 마우스 클릭 지원).
  - `4`: **Scrollable** (커스텀 Panorama HUD: `W`/`S` 이동, `A`/`D` 페이지, `E` 선택, `R` 닫기).

## 🖼️ 메뉴 미리보기

| Built-In (`MenuType = 1`) | Keypress (`MenuType = 2`) | Clickable (`MenuType = 3`) | Scrollable (`MenuType = 4`) |
| :---: | :---: | :---: | :---: |
| ![Built-In Rank](../built-in/rank.JPG) | ![Keypress Menu](../keypress/menu.JPG) | ![Clickable Menu](../clickable/menu.JPG) | ![Scrollable Menu](../scrollable/menu.JPG) |
