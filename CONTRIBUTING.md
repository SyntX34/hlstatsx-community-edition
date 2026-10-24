# Contributing to HLstatsX:CE

Contributions, bug reports, security fixes, and performance improvements are welcome. This repository contains three main runtime components:

1. **Web Interface (`web/`, `sql/`)** — PHP 8.1–8.5+ frontend, admin panel, and incremental schema updaters (`web/updater/`).
2. **Perl Daemon (`scripts/`)** — Real-time UDP log parser (`hlstats.pl`), proxy daemon (`proxy-daemon.pl`), and server command dispatcher (`HLstats_Server.pm`).
3. **SwiftlyS2 CS2 Plugin (`swiftlys2/`)** — C# (.NET 10) plugin (`HLStatsX`) and shared contract (`HLStatsX.Contract`) for Counter-Strike 2 servers.

## Development Standards

### 1. PHP Web Interface (`web/`)
- Target **PHP 8.1 through PHP 8.5+** compatibility.
- Always sanitize and escape inputs passed to SQL queries using `$db->escape()` or explicit integer casting (`(int)`).
- Always provide explicit `$escape = "\\"` when calling `fgetcsv()` or `fputcsv()` for PHP 8.4/8.5 compatibility.
- When introducing database schema or version changes, add a new numbered migration script in `web/updater/` (for example, `web/updater/84.php`).
- Lint all modified PHP files before committing:
  ```bash
  php -l web/hlstats.php
  ```

### 2. Perl Daemon (`scripts/`)
- Avoid busy-wait loops or unthrottled per-packet database queries.
- Use `execCached()` for frequently executed parameterized SQL queries.
- Keep `printEvent()` and `printNotice()` output structured and single-line so operators running the daemon under `systemd` can inspect logs cleanly with `journalctl -u hlstatsx -f`.
- Check Perl syntax before committing:
  ```bash
  perl -Iscripts -c scripts/hlstats.pl
  ```

### 3. SwiftlyS2 CS2 Plugin (`swiftlys2/HLStatsX/`)
- Keep plugin source organized by responsibility inside `swiftlys2/HLStatsX/src/`:
  - `HLStatsX.cs` — Plugin entry point, shared API (`IHLStatsXApi`), lifecycle (`Load` / `Unload`).
  - `Core/Config.cs` — Configuration model (`config.toml`) and layout path normalization.
  - `Core/UdpConnection.cs` — UDP log sender, async `HLX_CMD` receiver, and daemon command execution.
  - `Core/Commands.cs` — Server commands (`hlx_sm_*`) and player chat/console stat triggers.
  - `Core/Function.cs` — Game event hooks, weapon/hitgroup stat aggregation, `Core.Menu` (`IMenuService`) menus, and CustomHud Panorama rendering.
- Use `/* ... */` block comments in C#, PHP, and JS files.
- Ensure `dotnet publish swiftlys2/HLStatsX/HLStatsX.csproj -c Release` succeeds with **0 warnings and 0 errors**.

## Pull Request Workflow

1. Create a feature or bugfix branch from `main`.
2. Make focused changes that stay within the scope of the issue or feature.
3. Verify builds and syntax checks across all modified components (`dotnet publish`, `php -l`, `perl -c`).
4. Write a descriptive Conventional Commit message (`feat:`, `fix:`, `perf:`, `docs:`) explaining what changed and why.
5. Open a Pull Request with a summary of the changes and test steps.
