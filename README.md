# macsnip-win

[![GitHub Release](https://img.shields.io/github/v/release/Hanzz258/macsnip-win?color=2563eb)](https://github.com/Hanzz258/macsnip-win/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows 10/11](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](#requirements)
[![Binary Size: ~39 KB](https://img.shields.io/badge/Binary%20Size-~39%20KB-16a34a.svg)](#ultra-lightweight-by-design)
[![Idle Memory: < 1 MB](https://img.shields.io/badge/Idle%20Memory-~0.5%20MB-16a34a.svg)](#ultra-lightweight-by-design)
[![Idle CPU: 0%](https://img.shields.io/badge/Idle%20CPU-0%25-16a34a.svg)](#ultra-lightweight-by-design)
[![Dependencies: Zero](https://img.shields.io/badge/Dependencies-Zero-brightgreen.svg)](#highlights)

> Bring macOS screenshot muscle memory (`⌘ + ⇧ + 4` / `⌘ + ⇧ + 5`) to Windows — **~39 KB** native executable, **~0.5 MB** idle RAM, **0% CPU**, zero external dependencies.
>
> 为习惯 macOS 截图快捷键的用户打造的 Windows 极致轻量截图热键工具——仅 **~39 KB** 单文件原生程序、静默内存约 **0.5 MB**、**0% CPU**、零依赖、支持隐藏系统托盘纯后台静默运行。

[English](#english) | [中文说明](#中文说明)

---

## English

### Why macsnip-win?

If you switch between macOS and Windows every day, your fingers probably hit `Shift + 4` or `Shift + 5` out of habit whenever you need a screenshot—instead of Windows' `Win + Shift + S`.

Instead of installing a bloated screenshot suite, running an Electron app, or maintaining AutoHotkey scripts with global keyboard hooks, **macsnip-win** maps macOS-style shortcuts directly onto Windows' native Snipping Tool (`ms-screenclip:`) in a **~39 KB** standalone Win32 GUI binary compiled with the `.NET Framework` compiler already built into Windows.

### Ultra-Lightweight by Design

| Metric | Measured Value (Windows 11) | How It Achieves This |
| :--- | :--- | :--- |
| **Executable Size** | **~39 KB** (`MacSnip.exe`) | Single-file C# compiled to native Win32 GUI (`/target:winexe`, `AnyCPU`), zero bundled DLLs |
| **Idle Memory (Task Manager)** | **~0.5 MB** (~0.3 MB tray hidden) | Minimal WinForms footprint + one-shot startup working-set trim (`SetProcessWorkingSetSize`) |
| **Idle CPU Usage** | **0%** | Pure Win32 `RegisterHotKey` + `WM_HOTKEY` event-driven loop — **no polling, no low-level keyboard hooks** |
| **Startup Impact** | **Instant (~0.1s CPU)** | No console flash, no splash screen, no background service, no network access |
| **External Dependencies** | **Zero** | No AutoHotkey, Python, Node.js, or third-party runtimes required |
| **Permissions** | **Standard User** | No administrator privileges required; clean per-user install & uninstall |

> Unlike tools that install global `WH_KEYBOARD_LL` hooks (intercepting every single keystroke you type), `macsnip-win` only asks Windows to notify it when your two screenshot shortcuts are pressed—keeping latency, CPU, and privacy footprint virtually zero.

### Shortcuts

| Windows Hotkey | macOS Inspiration | Action | Underlying Windows URI |
| :--- | :--- | :--- | :--- |
| **`Ctrl + Shift + 4`** *(top-row `4`)* | `⌘ + ⇧ + 4` | **Rectangle Snip** (drag to select region) | `ms-screenclip:?clippingMode=Rectangle` |
| **`Ctrl + Shift + 5`** *(top-row `5`)* | `⌘ + ⇧ + 5` | **Window Snip** (click any app window) | `ms-screenclip:?clippingMode=Window` |

> **Tip:** Uses top-row number keys (`4` / `5`), not numpad keys. Double-clicking the tray icon also triggers **Rectangle Snip**.

### Highlights

- **Featherweight background daemon**: ~39 KB executable, ~0.5 MB resident RAM in Task Manager, 0% idle CPU, zero background timers when healthy.
- **Optional headless mode (Show / Hide Tray Icon)**: Toggle **Show Tray Icon** directly in the tray menu (or pass `--hide-tray` / `--show-tray`) to keep your taskbar completely clean while shortcuts remain active—persisted across reboots. Launch `MacSnip.exe` again anytime to instantly restore the tray icon.
- **Native Windows Snipping Tool**: Directly invokes `ms-screenclip:`, preserving clipboard copy, annotations, OCR, and auto-save to `Pictures\Screenshots`.
- **Auto-localized Tray UI**: Automatically adapts to English or Simplified Chinese based on your Windows display language (overridable via `--lang`).
- **Per-Monitor V2 DPI aware**: Crisp tray menus and notifications across mixed-DPI multi-monitor setups.
- **Reliable autostart & conflict handling**: Automatically retries transient hotkey registration errors at boot, stays in sync with Windows Task Manager (`StartupApproved`), and alerts you with clear error details if another app occupies a shortcut.

### Requirements

- **Windows 10 (1809 or later) / Windows 11**
- **Windows Snipping Tool** (built-in `ms-screenclip:` URI handler)
- **No administrator privileges required**

### Quick Start

#### Option 1: Download Prebuilt Binary (Recommended)

1. Download `MacSnip.exe` from the [Releases](https://github.com/Hanzz258/macsnip-win/releases/latest) page.
2. Place `MacSnip.exe` in a permanent folder (e.g., `C:\Tools\MacSnip\`) and double-click to run.
3. Right-click the tray icon (inside the `^` overflow area) to toggle **Launch at Startup** or **Show Tray Icon**.

#### Option 2: Build & Install from Source

1. Clone this repository:
   ```powershell
   git clone https://github.com/Hanzz258/macsnip-win.git
   cd macsnip-win
   ```
2. Build, enable autostart, and launch in one command:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
   Optional flags for `install.ps1`:
   - `-HideTray`: install and run silently without a tray icon
   - `-NoAutostart`: build and launch without creating a Startup shortcut
   - `-NoLaunch`: build and configure Startup without launching immediately

### Build Only & Uninstall

- **Build only** (generates `app.ico` and `MacSnip.exe`):
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\build.ps1
  ```
  Optional flags: `-RebuildIcon` (regenerate `app.ico`), `-WarningsAsErrors` (strict `/warn:4 /warnaserror+` check).

- **Stop & remove from Startup**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
  ```
  Optional flag: `-RemoveBinaries` (also deletes `MacSnip.exe`, `app.ico`, and local `error.log`).

### Command-Line Options

| Flag | Description |
| :--- | :--- |
| `--startup` | Silent start (suppresses the initial "MacSnip Ready" notification; respects saved tray visibility preference) |
| `--show-tray` | Show the system tray icon and save this preference (works both on launch and on an already-running instance) |
| `--hide-tray` | Hide the system tray icon and save this preference (works both on launch and on an already-running instance) |
| `--stop` | Gracefully stops the currently running `MacSnip.exe` instance and waits for clean exit |
| `--lang en` / `--lang zh` | Overrides automatic Windows UI language detection (`--lang=en` / `--lang=zh` also supported) |

### Troubleshooting / FAQ

- **I hid the system tray icon — how do I bring it back?**
  Simply **double-click `MacSnip.exe`** again, or run `.\MacSnip.exe --show-tray`. The running background instance immediately restores its tray icon and saves the preference.

- **Hotkey conflict (`Win32 error 1409` / `ERROR_HOTKEY_ALREADY_REGISTERED`)**
  Another background app has already claimed `Ctrl + Shift + 4` or `Ctrl + Shift + 5` (common culprits include QQ, WeChat, Snipaste, PowerToys, ShareX, or GPU overlays). Free or reassign the shortcut in that app, then right-click the MacSnip tray icon and click **Re-register Hotkeys**.

- **Windows SmartScreen warning when running a downloaded `MacSnip.exe`**
  Because open-source executables without an expensive EV code-signing certificate are initially unrecognized by SmartScreen, Windows may display a prompt on first launch. You can inspect the single-file source (`MacSnip.cs`) and compile locally in 1 second with `.\build.ps1`, or choose *More info → Run anyway*.

- **Nothing happens when triggering a screenshot**
  Ensure Windows Snipping Tool is installed and enabled. Press `Win + R` and run `ms-screenclip:?clippingMode=Rectangle` to verify. If missing (e.g., on stripped Windows builds), reinstall **Snipping Tool** from the Microsoft Store.

- **Where are settings and error logs stored?**
  - Tray visibility preference: `HKCU\Software\MacSnip` (`ShowTrayIcon` DWORD, cleaned automatically by `uninstall.ps1`).
  - Error log: `error.log` next to `MacSnip.exe` (rotated at 256 KB), falling back to `%LOCALAPPDATA%\MacSnip\error.log` if the app directory is read-only.

### Project Structure

```text
macsnip-win/
├── MacSnip.cs           # Single-file Win32/WinForms source (RegisterHotKey + bilingual tray UI)
├── build.ps1            # Generates app.ico and compiles MacSnip.exe via built-in csc.exe
├── install.ps1          # Builds, configures Startup (+ StartupApproved), and launches MacSnip.exe
├── uninstall.ps1        # Gracefully stops MacSnip.exe and cleans up Startup & registry state
├── CHANGELOG.md         # Release history
├── CONTRIBUTING.md      # Contribution & local development guide
├── LICENSE              # MIT License
└── README.md
```

### Contributing

Contributions, bug reports, and suggestions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a pull request.

### License

Released under the [MIT License](LICENSE).

---

## 中文说明

### 为什么做 macsnip-win？

很多同时使用 macOS 和 Windows 的开发者或设计师，截图时肌肉记忆总是下意识去按 `⌘ + ⇧ + 4` / `⌘ + ⇧ + 5`，切回 Windows 按 `Win + Shift + S` 往往很别扭。

如果你不想为了两个快捷键去装臃肿的第三方截图套件、Electron 工具，也不想维护 AutoHotkey 脚本和全局键盘钩子，**macsnip-win** 就是最干净的原生方案：它使用 Windows 自带的 `.NET Framework` 编译器编成仅 **~39 KB** 的独立原生程序，直接把快捷键映射到系统原生截图工具。

### 极致轻量与超低占用

| 指标 | 实测表现（Windows 11） | 实现方式 |
| :--- | :--- | :--- |
| **程序体积** | **仅 ~39 KB**（单文件 `MacSnip.exe`） | 单文件 C# 编译为原生 Win32 GUI（`/target:winexe`、`AnyCPU`），无任何附带 DLL |
| **任务管理器内存占用** | **约 0.5 MB**（隐藏托盘时约 **0.3 MB**） | 极简原生消息循环 + 启动就绪后自动释放多余工作集内存（`SetProcessWorkingSetSize`） |
| **静默 CPU 占用** | **0%** | 纯 Win32 `RegisterHotKey` + `WM_HOTKEY` 事件驱动，**无后台轮询、无全局键盘钩子** |
| **开机与启动开销** | **瞬时启动（约 0.1s CPU）** | 无黑框闪烁、无开机弹窗、无后台系统服务、无任何联网请求 |
| **外部依赖** | **零依赖** | 无需安装 AutoHotkey、Python、Node.js 或任何第三方运行库 |
| **权限需求** | **普通用户权限** | 无需管理员权限，安装与卸载干净无残留 |

> 相比通过全局低级键盘钩子（`WH_KEYBOARD_LL`）拦截每一次敲键的方案，`macsnip-win` 仅向系统注册两个热键，平时完全休眠，在响应延迟、电量功耗与输入隐私上都做到极致克制。

### 快捷键一览

| Windows 快捷键 | 对应 macOS 习惯 | 功能 | 调用的系统协议 |
| :--- | :--- | :--- | :--- |
| **`Ctrl + Shift + 4`**（主键盘数字 `4`） | `⌘ + ⇧ + 4` | **框选截图**（自由拖拽矩形区域） | `ms-screenclip:?clippingMode=Rectangle` |
| **`Ctrl + Shift + 5`**（主键盘数字 `5`） | `⌘ + ⇧ + 5` | **应用窗口截图**（悬停并点击任意窗口） | `ms-screenclip:?clippingMode=Window` |

> **提示**：仅响应主键盘顶部数字键 `4` / `5`（不占用小键盘）；双击托盘图标也可直接触发**框选截图**。

### 核心特性

- **极致轻量、几乎零存在感**：~39 KB 体积、任务管理器常驻约 0.5 MB 内存、静默 0% CPU，长期后台常驻毫无负担。
- **支持自由显示/隐藏系统托盘图标**：右键托盘菜单可取消勾选**在系统托盘中显示**（或使用 `--hide-tray` / `--show-tray`），让程序完全隐身于后台且开机自动保持该设置；随时再次双击 `MacSnip.exe` 即可一键恢复托盘图标，绝不会“藏丢”。
- **完美衔接系统原生截图**：直接唤起 Windows 原生截图遮罩，自动复制剪贴板、支持标注重绘、OCR 与自动保存到 `图片\屏幕截图`。
- **中英文自动适配**：托盘菜单与气泡提示随 Windows 显示语言自动切换中/英文（亦支持 `--lang` 手动指定）。
- **高分屏清晰渲染**：适配 Per-Monitor V2 DPI，在多显示器不同缩放比例下托盘菜单依然清晰锐利。
- **稳定自启与冲突提示**：开机阶段自动重试注册；自启开关与任务管理器 `StartupApproved` 状态同步；快捷键冲突时会明确提示占用键位与 Win32 错误码。

### 系统要求

- **Windows 10（1809 及以上）或 Windows 11**
- 系统已安装原生 **截图工具（Snipping Tool）**
- **无需管理员权限**

### 快速开始

#### 方式一：直接下载预编译版本（推荐）

1. 前往 [Releases](https://github.com/Hanzz258/macsnip-win/releases/latest) 页面下载 `MacSnip.exe`。
2. 将 `MacSnip.exe` 放到固定目录（如 `D:\Tools\MacSnip\`）后双击运行。
3. 右键任务栏右下角托盘图标（通常收纳在 `^` 折叠区），按需勾选**开机自动启动**或切换**在系统托盘中显示**。

#### 方式二：从源码一键编译并安装

1. 克隆仓库：
   ```powershell
   git clone https://github.com/Hanzz258/macsnip-win.git
   cd macsnip-win
   ```
2. 一键编译、设置开机自启并启动（无需管理员权限）：
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
   `install.ps1` 可选参数：
   - `-HideTray`：安装并以隐藏系统托盘的纯后台静默模式运行
   - `-NoAutostart`：仅编译并运行，不写入开机启动项
   - `-NoLaunch`：仅编译并配置自启，不立即运行

### 单独编译与卸载

- **仅编译生成 `MacSnip.exe`**（会自动生成 `app.ico`）：
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\build.ps1
  ```
  可选参数：`-RebuildIcon`（强制重新生成图标）、`-WarningsAsErrors`（将所有编译警告视为错误）。

- **停止后台并取消开机自启**：
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
  ```
  可选参数：`-RemoveBinaries`（同时清理 `MacSnip.exe`、`app.ico` 与本地 `error.log`）。

### 命令行参数

| 参数 | 说明 |
| :--- | :--- |
| `--startup` | 静默启动（不弹出初始“MacSnip 已就绪”气泡提示，遵循已保存的托盘显示设置） |
| `--show-tray` | 显示系统托盘图标并保存偏好（支持冷启动及向已运行实例实时生效） |
| `--hide-tray` | 隐藏系统托盘图标并保存偏好（支持冷启动及向已运行实例实时生效） |
| `--stop` | 优雅停止正在后台运行的 `MacSnip.exe` 实例并等待退出完成 |
| `--lang zh` / `--lang en` | 手动指定托盘界面语言（也支持 `--lang=zh` / `--lang=en` 写法） |

### 常见问题（FAQ）

- **隐藏系统托盘图标后，怎么重新显示出来？**
  直接**再次双击运行 `MacSnip.exe`**，或在终端执行 `.\MacSnip.exe --show-tray`，后台运行中的实例会立即恢复托盘图标并更新偏好设置。

- **提示快捷键冲突（Win32 错误码 `1409`）怎么办？**
  错误码 `1409` 表示对应快捷键已被其他常驻软件抢占（常见如 QQ、微信、Snipaste、PowerToys、ShareX 或显卡热键）。释放对应软件的热键后，右键托盘图标点击**重新注册快捷键**即可立即恢复。

- **下载的 `MacSnip.exe` 触发 Windows SmartScreen 拦截提示？**
  个人开源项目未购买商业 EV 代码签名证书，首次运行直接下载的 `.exe` 可能会出现 SmartScreen 提示。由于本项目核心源码仅单个 `MacSnip.cs` 文件，推荐直接审阅源码并在本地用 `.\build.ps1` 一秒自行编译，或在 SmartScreen 中点击“更多信息 → 仍要运行”。

- **按下快捷键后没有弹出截图界面？**
  请按 `Win + R` 输入 `ms-screenclip:?clippingMode=Rectangle` 回车测试系统截图协议是否正常。如系统精简版移除了截图工具，请通过 Microsoft Store 重新安装 **截图工具（Snipping Tool）**。

- **设置与日志保存在哪里？**
  - 托盘显隐偏好保存在 `HKCU\Software\MacSnip`（`ShowTrayIcon`），运行 `uninstall.ps1` 时会自动清理。
  - 异常日志默认记录在 `MacSnip.exe` 同目录的 `error.log`（超过 256 KB 自动轮转备份）；若所在目录只读（如 `Program Files`），会自动回退至 `%LOCALAPPDATA%\MacSnip\error.log`。

### 参与贡献与开源协议

欢迎提交 Issue 或 Pull Request，详情请参阅 [CONTRIBUTING.md](CONTRIBUTING.md)。

本项目基于 [MIT License](LICENSE) 开源。
