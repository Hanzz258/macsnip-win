# Contributing to macsnip-win

Thank you for considering contributing to **macsnip-win**!

## Design Principles

Please keep these core goals in mind when proposing changes:

1. **Zero external dependencies**: The project must compile out of the box on a clean Windows 10/11 machine using only the pre-installed `.NET Framework 4.x` compiler (`csc.exe`) and Windows PowerShell 5.1.
2. **C# 5.0 syntax compatibility**: Built-in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` implements C# 5.0. Avoid C# 6+ syntax features (such as `$"..."` string interpolation, `?.`, `nameof`, expression-bodied members, or `out var`).
3. **Pure ASCII PowerShell scripts**: Keep `*.ps1` scripts pure ASCII so they run reliably under Windows PowerShell 5.1 regardless of UTF-8 BOM or system codepage settings.
4. **Stay ultra-lightweight and focused**: Avoid background polling loops, global keyboard hooks (`WH_KEYBOARD_LL`), heavy UI frameworks, or unnecessary permissions.

## Local Development & Verification

1. Clone the repository:
   ```powershell
   git clone https://github.com/Hanzz258/macsnip-win.git
   cd macsnip-win
   ```

2. Compile with strict warnings enabled:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1 -WarningsAsErrors
   ```

3. Smoke-test tray visibility switching, localization, and graceful shutdown:
   ```powershell
   Start-Process .\MacSnip.exe -ArgumentList '--lang en'
   .\MacSnip.exe --hide-tray
   .\MacSnip.exe --show-tray
   .\MacSnip.exe --stop
   Start-Process .\MacSnip.exe -ArgumentList '--lang zh'
   .\MacSnip.exe --stop
   ```

4. Verify hotkeys (`Ctrl+Shift+4` and `Ctrl+Shift+5`), tray menu actions, and autostart toggle on Windows 10 or Windows 11.

## Submitting Pull Requests

- Keep PRs focused on a single bug fix or enhancement.
- Update `README.md` (both English and Chinese sections) and `CHANGELOG.md` if your change affects user-facing behavior.
- Ensure `.\build.ps1 -WarningsAsErrors` exits with code `0` and produces no compiler warnings.
- Do not commit generated binaries (`MacSnip.exe`, `app.ico`) or runtime logs (`error.log`).
