# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-25

### Added
- Native Windows global hotkey daemon mapping `Ctrl+Shift+4` (Rectangle Snip) and `Ctrl+Shift+5` (Window Snip) directly to Windows Snipping Tool (`ms-screenclip:`).
- Ultra-lightweight single-file executable (~39 KB, ~0.5 MB resident memory in Task Manager, 0% idle CPU) with automatic startup working-set trimming (`SetProcessWorkingSetSize`).
- Persistent **Show Tray Icon** toggle in the tray menu with confirmation prompt, instant restore by re-launching `MacSnip.exe`, plus `--show-tray` / `--hide-tray` CLI flags (both cold start and live IPC switching) and `install.ps1 -HideTray`.
- Automatic English / Simplified Chinese UI localization based on Windows display language, plus `--lang en` / `--lang zh` CLI override.
- Single-instance mutex guard with `--startup` (silent boot) and `--stop` (graceful shutdown) CLI flags.
- Per-Monitor V2 DPI awareness (`SetProcessDpiAwarenessContext`) with graceful fallback to `SetProcessDPIAware`.
- Boot-time hotkey registration retry timer, human-readable Win32 error `1409` conflict notices, and full sync between the tray **Launch at Startup** toggle and Windows Task Manager (`StartupApproved\StartupFolder`).
- 64-bit monotonic `Stopwatch` debounce (immune to 32-bit `Environment.TickCount` 24.9-day wrap-around) and auto-rotating `error.log` with `%LOCALAPPDATA%\MacSnip\error.log` fallback for read-only directories.
- Zero-dependency PowerShell scripts: `build.ps1` (programmatic multi-resolution `app.ico` generator + built-in `.NET Framework` `csc.exe` compilation), `install.ps1`, and `uninstall.ps1`.
