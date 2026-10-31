## HLstatsX Community Edition

### 📌 Requirements

- **PHP:** Patched October 2026 release required:
  - **PHP 8.5.12** or higher
  - **PHP 8.4.27** or higher
  - **PHP 8.3.36** or higher
  - **PHP 8.2.35** or higher
- **MySQL / MariaDB:** version 5.7 or higher required (recommended MySQL 8.0+ / MariaDB 10.6+).
- **Perl:** version 5.26+ for the real-time log daemon (`scripts/hlstats.pl`).
- **.NET SDK:** version 10.0+ for building the Counter-Strike 2 SwiftlyS2 plugin (`swiftlys2/HLStatsX`).
---
HLstatsX Community Edition is an open-source project licensed
under GNU General Public License v2 and is a real-time stats
and ranking system for Source and Source 2 engine based games.
HLstatsX Community Edition uses a Perl daemon to parse the log
streamed from the game server. The data is stored in a MySQL
Database and has a PHP web frontend.


#### :loudspeaker: Important changes
| Date  | Description | Additional information |
| ------------- | ------------- | ------------- |
| 24.10.2026  | Applied October 2026 PHP security & compatibility patch (enforcing PHP 8.2.35+, 8.3.36+, 8.4.27+, or 8.5.12+), web SQLi & session hardening, Perl daemon CPU/systemd logging optimization, and SwiftlyS2 CS2 plugin v1.2.0 modular overhaul | Added `web/updater/84.php` (`v1.10.0`, `dbversion = 84`), migrated SwiftlyS2 plugin to `Core.Menu` (`IMenuService`), fixed CustomHud Panorama paths, and added multi-map transition & memory cleanup |
| 17.09.2026  | Added native Counter-Strike 2 (CS2) support and SwiftlyS2 in-game server plugin | Added `cs2` game code, schema migrations, and SwiftlyS2 plugin in `swiftlys2/HLStatsX` |
| 07.01.2020  | [#45](https://github.com/NomisCZ/hlstatsx-community-edition/issues/45) GeoIP2 Linux script updated, GeoLite2 MaxMind database (GDPR and CCPA) | https://blog.maxmind.com/2019/12/18/significant-changes-to-accessing-and-using-geolite2-databases/ |

> Date format: DD.MM.YYYY
---

### :video_game: Counter-Strike 2 (SwiftlyS2) Setup, Commands & Debugging Guide
* **[CS2 Setup, Configuration, Proxy_Key & UDP Debugging Guide (CS2.md)](CS2.md)** — Complete step-by-step instructions for Linux and Windows, including `config.toml`, Web Admin `SWIFTLYS2` & `Proxy_Key` configuration, `systemd` / `journalctl` service setup, and `tcpdump` / PowerShell UDP packet verification.
* **[SwiftlyS2 In-Game Plugin Documentation & Command Reference (swiftlys2/HLStatsX/README.md)](swiftlys2/HLStatsX/README.md)** — Complete reference for all in-game player commands (`!hlx`, `!rank`, `!top10`, `!top20`, `!next`, `!session`, `!statsme`, `!weapons`, `!accuracy`, `!targets`, `!kills`, `!actions`, `!maps`, per-weapon stats, auto-commands), `Core.Menu` navigation keybinds, daemon `HLX_CMD` commands, and the `HLStatsX.Contract` developer API.

> **Updating an Existing Site with Local Customizations:**
> If you have local changes or a custom theme on your HLstatsX web panel and want to review or apply only the patched files, check this commit:
> [https://github.com/SyntX34/hlstatsx-community-edition/commit/007b223ee403c738a6b18baf9ee1f63013ba5a7e#diff-0e1a79d051518c85ddb92bcceb0a68de79281ba4654ad474a41902dee3910eb0](https://github.com/SyntX34/hlstatsx-community-edition/commit/007b223ee403c738a6b18baf9ee1f63013ba5a7e#diff-0e1a79d051518c85ddb92bcceb0a68de79281ba4654ad474a41902dee3910eb0)

### :busts_in_silhouette: Contributors
* **SyntX34** — Counter-Strike 2 (SwiftlyS2) in-game plugin & API, October 2026 PHP security/compatibility patch, and Perl daemon performance & `systemd`/`journalctl` logging optimizations.
* **NomisCZ** & the **HLstatsX Community Edition** contributors.

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines and pull request instructions.

### :book: Documentation
* [CS2.md](CS2.md) — Counter-Strike 2 (SwiftlyS2) Installation & Debugging Guide
* [swiftlys2/HLStatsX/README.md](swiftlys2/HLStatsX/README.md) — SwiftlyS2 CS2 Plugin Commands, Menu Controls & Developer API
* [CONTRIBUTING.md](CONTRIBUTING.md) — Contribution & Development Guidelines
* https://github.com/NomisCZ/hlstatsx-community-edition/wiki 🚧 Wiki - work in progress 🚧
### :speech_balloon: Help
*  https://forums.alliedmods.net/forumdisplay.php?f=156
---
