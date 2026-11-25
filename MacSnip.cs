using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("MacSnip for Windows")]
[assembly: AssemblyDescription("macOS-style Ctrl+Shift+4 (Rectangle) and Ctrl+Shift+5 (Window) screenshot hotkeys for Windows")]
[assembly: AssemblyProduct("macsnip-win")]
[assembly: AssemblyCopyright("Copyright (c) 2026 韩渣渣")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace MacSnipWin
{
    internal static class NativeMethods
    {
        public const int HOTKEY_ID_RECT = 1;
        public const int HOTKEY_ID_WINDOW = 2;

        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint VK_4 = 0x34; // Main keyboard top-row '4' key (NOT numpad 4)
        public const uint VK_5 = 0x35; // Main keyboard top-row '5' key (NOT numpad 5)

        public const int WM_HOTKEY = 0x0312;
        public const int WM_APP_EXIT = 0x8001;
        public const int WM_APP_PING = 0x8002;
        public const int WM_APP_SHOW_TRAY = 0x8003;
        public const int WM_APP_HIDE_TRAY = 0x8004;

        public const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        public static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    return;
                }
            }
            catch (EntryPointNotFoundException)
            {
            }
            catch (DllNotFoundException)
            {
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        public static void TrimWorkingSet()
        {
            try
            {
                using (Process current = Process.GetCurrentProcess())
                {
                    SetProcessWorkingSetSize(current.Handle, new IntPtr(-1), new IntPtr(-1));
                }
            }
            catch
            {
            }
        }
    }

    internal sealed class UiStrings
    {
        public readonly bool IsChinese;

        public readonly string StatusRegistering;
        public readonly string StatusRunning;
        public readonly string StatusRectOkWindowConflictFormat;
        public readonly string StatusWindowOkRectConflictFormat;
        public readonly string StatusBothConflictFormat;

        public readonly string TrayDefault;
        public readonly string TrayRunning;
        public readonly string TrayConflict4;
        public readonly string TrayConflict5;
        public readonly string TrayConflictBoth;

        public readonly string MenuRectSnip;
        public readonly string MenuWindowSnip;
        public readonly string MenuAutostart;
        public readonly string MenuShowTray;
        public readonly string MenuReregister;
        public readonly string MenuExit;

        public readonly string LabelRectSnip;
        public readonly string LabelWindowSnip;

        public readonly string BalloonReadyTitle;
        public readonly string BalloonAlreadyRunningTitle;
        public readonly string BalloonTrayRestoredTitle;
        public readonly string BalloonShortcutsBody;
        public readonly string BalloonConflictTitle;
        public readonly string BalloonPartialConflictTitle;
        public readonly string BalloonConflictHint;
        public readonly string BalloonLaunchFailedTitleFormat;
        public readonly string BalloonLaunchFailedBodyFormat;

        public readonly string ConflictBothFormat;
        public readonly string ConflictRectOnlyFormat;
        public readonly string ConflictWindowOnlyFormat;

        public readonly string ConfirmHideTrayMessage;
        public readonly string AutostartToggleErrorFormat;

        private UiStrings(bool isChinese)
        {
            IsChinese = isChinese;

            if (isChinese)
            {
                StatusRegistering = "状态：正在注册快捷键...";
                StatusRunning = "状态：运行中 (Ctrl+Shift+4 框选 / 5 窗口)";
                StatusRectOkWindowConflictFormat = "状态：4 正常，5 冲突 ({0})";
                StatusWindowOkRectConflictFormat = "状态：5 正常，4 冲突 ({0})";
                StatusBothConflictFormat = "状态：4/5 均冲突 ({0} / {1})";

                TrayDefault = "MacSnip (Ctrl+Shift+4/5)";
                TrayRunning = "MacSnip (Ctrl+Shift+4 框选 / 5 窗口)";
                TrayConflict4 = "MacSnip (Ctrl+Shift+4 冲突)";
                TrayConflict5 = "MacSnip (Ctrl+Shift+5 冲突)";
                TrayConflictBoth = "MacSnip (快捷键冲突)";

                MenuRectSnip = "框选截图 (Ctrl+Shift+4)";
                MenuWindowSnip = "应用窗口截图 (Ctrl+Shift+5)";
                MenuAutostart = "开机自动启动";
                MenuShowTray = "在系统托盘中显示";
                MenuReregister = "重新注册快捷键";
                MenuExit = "退出";

                LabelRectSnip = "框选截图";
                LabelWindowSnip = "应用窗口截图";

                BalloonReadyTitle = "MacSnip 已就绪";
                BalloonAlreadyRunningTitle = "MacSnip 已在后台运行";
                BalloonTrayRestoredTitle = "MacSnip 托盘图标已恢复显示";
                BalloonShortcutsBody = "Ctrl+Shift+4：框选截图\nCtrl+Shift+5：应用窗口截图";
                BalloonConflictTitle = "MacSnip 快捷键冲突提示";
                BalloonPartialConflictTitle = "MacSnip：快捷键注册失败";
                BalloonConflictHint = "可右键右下角托盘图标点击“重新注册快捷键”。";
                BalloonLaunchFailedTitleFormat = "无法启动{0}";
                BalloonLaunchFailedBodyFormat = "调用 Windows 截图组件失败：{0}";

                ConflictBothFormat = "Ctrl+Shift+4 ({0}) 与 Ctrl+Shift+5 ({1}) 注册失败";
                ConflictRectOnlyFormat = "Ctrl+Shift+4 ({0}) 注册失败，Ctrl+Shift+5 已正常启用";
                ConflictWindowOnlyFormat = "Ctrl+Shift+5 ({0}) 注册失败，Ctrl+Shift+4 已正常启用";

                ConfirmHideTrayMessage =
                    "即将隐藏系统托盘图标，快捷键（Ctrl+Shift+4 / Ctrl+Shift+5）将继续在后台静默生效。\n\n" +
                    "如需重新显示托盘图标，只需再次双击运行 MacSnip.exe（或执行 MacSnip.exe --show-tray）。\n\n" +
                    "确认隐藏吗？";
                AutostartToggleErrorFormat = "切换开机自启失败：{0}";
            }
            else
            {
                StatusRegistering = "Status: Registering hotkeys...";
                StatusRunning = "Status: Running (Ctrl+Shift+4 Rect / 5 Window)";
                StatusRectOkWindowConflictFormat = "Status: 4 OK, 5 conflict ({0})";
                StatusWindowOkRectConflictFormat = "Status: 5 OK, 4 conflict ({0})";
                StatusBothConflictFormat = "Status: 4 & 5 conflict ({0} / {1})";

                TrayDefault = "MacSnip (Ctrl+Shift+4/5)";
                TrayRunning = "MacSnip (Ctrl+Shift+4 Rect / 5 Window)";
                TrayConflict4 = "MacSnip (Ctrl+Shift+4 conflict)";
                TrayConflict5 = "MacSnip (Ctrl+Shift+5 conflict)";
                TrayConflictBoth = "MacSnip (Hotkey conflict)";

                MenuRectSnip = "Rectangle Snip (Ctrl+Shift+4)";
                MenuWindowSnip = "Window Snip (Ctrl+Shift+5)";
                MenuAutostart = "Launch at Startup";
                MenuShowTray = "Show Tray Icon";
                MenuReregister = "Re-register Hotkeys";
                MenuExit = "Exit";

                LabelRectSnip = "Rectangle Snip";
                LabelWindowSnip = "Window Snip";

                BalloonReadyTitle = "MacSnip Ready";
                BalloonAlreadyRunningTitle = "MacSnip is Already Running";
                BalloonTrayRestoredTitle = "MacSnip Tray Icon Restored";
                BalloonShortcutsBody = "Ctrl+Shift+4: Rectangle Snip\nCtrl+Shift+5: Window Snip";
                BalloonConflictTitle = "MacSnip Hotkey Conflict";
                BalloonPartialConflictTitle = "MacSnip: Hotkey Registration Failed";
                BalloonConflictHint = "Right-click the tray icon and choose \"Re-register Hotkeys\".";
                BalloonLaunchFailedTitleFormat = "Failed to Launch {0}";
                BalloonLaunchFailedBodyFormat = "Could not invoke Windows Snipping Tool: {0}";

                ConflictBothFormat = "Ctrl+Shift+4 ({0}) and Ctrl+Shift+5 ({1}) failed to register";
                ConflictRectOnlyFormat = "Ctrl+Shift+4 ({0}) failed to register; Ctrl+Shift+5 is active";
                ConflictWindowOnlyFormat = "Ctrl+Shift+5 ({0}) failed to register; Ctrl+Shift+4 is active";

                ConfirmHideTrayMessage =
                    "The system tray icon will be hidden while Ctrl+Shift+4 and Ctrl+Shift+5 stay active in the background.\n\n" +
                    "To show the tray icon again later, simply launch MacSnip.exe again (or run \"MacSnip.exe --show-tray\").\n\n" +
                    "Hide the tray icon now?";
                AutostartToggleErrorFormat = "Failed to toggle launch at startup: {0}";
            }
        }

        public static UiStrings Create(string languageOverride)
        {
            if (!string.IsNullOrEmpty(languageOverride))
            {
                if (languageOverride.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    return new UiStrings(true);
                }
                if (languageOverride.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return new UiStrings(false);
                }
            }

            bool isChinese = string.Equals(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                "zh",
                StringComparison.OrdinalIgnoreCase
            );
            return new UiStrings(isChinese);
        }

        public string FormatErrorShort(int errorCode)
        {
            if (errorCode == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED)
            {
                return IsChinese ? "已被占用, 1409" : "in use, 1409";
            }
            return IsChinese
                ? string.Format("错误码 {0}", errorCode)
                : string.Format("error {0}", errorCode);
        }

        public string FormatErrorDetail(int errorCode)
        {
            if (errorCode == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED)
            {
                return IsChinese ? "已被其他程序占用，错误码 1409" : "already in use by another app, error 1409";
            }
            return IsChinese
                ? string.Format("错误码 {0}", errorCode)
                : string.Format("Win32 error {0}", errorCode);
        }
    }

    internal sealed class MacSnipAppContext : ApplicationContext
    {
        private const string UriRectCapture = "ms-screenclip:?clippingMode=Rectangle";
        private const string UriWindowCapture = "ms-screenclip:?clippingMode=Window";
        private const string ShortcutFileName = "MacSnip.lnk";
        private const string StartupApprovedSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
        private const string AppSettingsSubKey = @"Software\MacSnip";
        private const string ShowTrayValueName = "ShowTrayIcon";
        private const long MaxLogFileBytes = 256 * 1024;
        private const int MaxNotifyIconTextLength = 63;

        private readonly UiStrings _strings;
        private readonly HotkeyMessageWindow _window;
        private readonly Icon _ownedAppIcon;
        private readonly ContextMenuStrip _menu;
        private readonly Font _boldMenuFont;
        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _statusItem;
        private readonly ToolStripMenuItem _autostartItem;
        private readonly ToolStripMenuItem _showTrayItem;
        private readonly System.Windows.Forms.Timer _retryTimer;
        private readonly System.Windows.Forms.Timer _trimTimer;
        private readonly EventWaitHandle _stopEvent;
        private readonly RegisteredWaitHandle _stopWaitHandle;
        private readonly EventWaitHandle _pingEvent;
        private readonly RegisteredWaitHandle _pingWaitHandle;
        private readonly EventWaitHandle _showTrayEvent;
        private readonly RegisteredWaitHandle _showTrayWaitHandle;
        private readonly EventWaitHandle _hideTrayEvent;
        private readonly RegisteredWaitHandle _hideTrayWaitHandle;
        private readonly Stopwatch _triggerWatch;

        private bool _trayVisible;
        private bool _rectRegistered;
        private bool _windowRegistered;
        private int _lastRectError;
        private int _lastWindowError;
        private int _retryCount;
        private long _lastTriggerMs;
        private bool _isShuttingDown;

        public const string MutexName = @"Local\MacSnipWin_SingleInstance";
        public const string StopEventName = @"Local\MacSnipWin_Stop";
        public const string PingEventName = @"Local\MacSnipWin_Ping";
        public const string ShowTrayEventName = @"Local\MacSnipWin_ShowTray";
        public const string HideTrayEventName = @"Local\MacSnipWin_HideTray";

        public MacSnipAppContext(bool silentStartup, string languageOverride, bool? trayOverride)
        {
            _strings = UiStrings.Create(languageOverride);
            _triggerWatch = Stopwatch.StartNew();
            _lastTriggerMs = -1000;
            _window = new HotkeyMessageWindow(this);

            if (trayOverride.HasValue)
            {
                _trayVisible = trayOverride.Value;
                SaveShowTrayPreference(_trayVisible);
            }
            else
            {
                _trayVisible = LoadShowTrayPreference();
            }

            Icon displayIcon = null;
            try
            {
                _ownedAppIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                displayIcon = _ownedAppIcon;
            }
            catch
            {
            }
            if (displayIcon == null)
            {
                displayIcon = SystemIcons.Application;
            }

            _menu = new ContextMenuStrip();

            _statusItem = new ToolStripMenuItem(_strings.StatusRegistering)
            {
                Enabled = false
            };

            ToolStripMenuItem rectItem = new ToolStripMenuItem(_strings.MenuRectSnip);
            _boldMenuFont = new Font(rectItem.Font, FontStyle.Bold);
            rectItem.Font = _boldMenuFont;
            rectItem.Click += delegate { TriggerScreenshot(UriRectCapture, _strings.LabelRectSnip); };

            ToolStripMenuItem windowItem = new ToolStripMenuItem(_strings.MenuWindowSnip);
            windowItem.Click += delegate { TriggerScreenshot(UriWindowCapture, _strings.LabelWindowSnip); };

            _autostartItem = new ToolStripMenuItem(_strings.MenuAutostart);
            _autostartItem.Click += delegate { ToggleAutostart(); };

            _showTrayItem = new ToolStripMenuItem(_strings.MenuShowTray)
            {
                Checked = _trayVisible
            };
            _showTrayItem.Click += delegate { ToggleTrayVisibilityFromMenu(); };

            ToolStripMenuItem retryItem = new ToolStripMenuItem(_strings.MenuReregister);
            retryItem.Click += delegate
            {
                _retryTimer.Stop();
                UnregisterAllHotkeysSafe();
                if (!TryRegisterHotkeys(true))
                {
                    ShowConflictBalloon(_strings.BalloonConflictTitle, false);
                }
            };

            ToolStripMenuItem exitItem = new ToolStripMenuItem(_strings.MenuExit);
            exitItem.Click += delegate { Shutdown(); };

            _menu.Opening += delegate
            {
                RefreshAutostartState();
                _showTrayItem.Checked = _trayVisible;
            };

            _menu.Items.Add(_statusItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(rectItem);
            _menu.Items.Add(windowItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_autostartItem);
            _menu.Items.Add(_showTrayItem);
            _menu.Items.Add(retryItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = displayIcon,
                ContextMenuStrip = _menu,
                Visible = _trayVisible
            };
            SetTrayText(_strings.TrayDefault);
            _trayIcon.DoubleClick += delegate { TriggerScreenshot(UriRectCapture, _strings.LabelRectSnip); };

            RefreshAutostartState();

            _retryTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _retryTimer.Tick += OnRetryTimerTick;

            if (!TryRegisterHotkeys(!silentStartup))
            {
                _retryCount = 0;
                _retryTimer.Start();
            }

            _trimTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _trimTimer.Tick += delegate
            {
                _trimTimer.Stop();
                NativeMethods.TrimWorkingSet();
            };
            _trimTimer.Start();

            _stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset, StopEventName);
            _stopWaitHandle = RegisterWindowMessageWait(_stopEvent, NativeMethods.WM_APP_EXIT);

            _pingEvent = new EventWaitHandle(false, EventResetMode.AutoReset, PingEventName);
            _pingWaitHandle = RegisterWindowMessageWait(_pingEvent, NativeMethods.WM_APP_PING);

            _showTrayEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowTrayEventName);
            _showTrayWaitHandle = RegisterWindowMessageWait(_showTrayEvent, NativeMethods.WM_APP_SHOW_TRAY);

            _hideTrayEvent = new EventWaitHandle(false, EventResetMode.AutoReset, HideTrayEventName);
            _hideTrayWaitHandle = RegisterWindowMessageWait(_hideTrayEvent, NativeMethods.WM_APP_HIDE_TRAY);
        }

        private RegisteredWaitHandle RegisterWindowMessageWait(EventWaitHandle waitHandle, uint messageId)
        {
            return ThreadPool.RegisterWaitForSingleObject(
                waitHandle,
                delegate
                {
                    IntPtr hwnd = _window.Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        NativeMethods.PostMessage(hwnd, messageId, IntPtr.Zero, IntPtr.Zero);
                    }
                },
                null,
                -1,
                false
            );
        }

        private void SetTrayText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _trayIcon.Text = string.Empty;
                return;
            }

            _trayIcon.Text = text.Length <= MaxNotifyIconTextLength
                ? text
                : text.Substring(0, MaxNotifyIconTextLength);
        }

        private void ShowBalloonIfVisible(int timeoutMs, string title, string text, ToolTipIcon icon)
        {
            if (_trayVisible)
            {
                _trayIcon.ShowBalloonTip(timeoutMs, title, text, icon);
            }
        }

        private static bool LoadShowTrayPreference()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AppSettingsSubKey, false))
                {
                    if (key == null)
                    {
                        return true;
                    }

                    object value = key.GetValue(ShowTrayValueName);
                    if (value is int)
                    {
                        return ((int)value) != 0;
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        private static void SaveShowTrayPreference(bool visible)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(AppSettingsSubKey))
                {
                    if (key != null)
                    {
                        key.SetValue(ShowTrayValueName, visible ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("SaveShowTrayPreference warning: " + ex.Message);
            }
        }

        private void SetTrayVisible(bool visible, bool persist)
        {
            _trayVisible = visible;
            _trayIcon.Visible = visible;
            _showTrayItem.Checked = visible;

            if (persist)
            {
                SaveShowTrayPreference(visible);
            }

            if (!visible)
            {
                NativeMethods.TrimWorkingSet();
            }
        }

        private void ToggleTrayVisibilityFromMenu()
        {
            if (_trayVisible)
            {
                DialogResult result = MessageBox.Show(
                    _strings.ConfirmHideTrayMessage,
                    "MacSnip",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );
                if (result == DialogResult.OK)
                {
                    SetTrayVisible(false, true);
                }
            }
            else
            {
                SetTrayVisible(true, true);
            }
        }

        public void OnShowTrayRequested()
        {
            SetTrayVisible(true, true);
            ShowBalloonIfVisible(
                2500,
                _strings.BalloonTrayRestoredTitle,
                _strings.BalloonShortcutsBody,
                ToolTipIcon.Info
            );
        }

        public void OnHideTrayRequested()
        {
            SetTrayVisible(false, true);
        }

        private void OnRetryTimerTick(object sender, EventArgs e)
        {
            _retryCount++;
            if (TryRegisterHotkeys(false))
            {
                _retryTimer.Stop();
                return;
            }

            if (_retryCount >= 6)
            {
                _retryTimer.Stop();
                ShowConflictBalloon(_strings.BalloonPartialConflictTitle, true);
            }
        }

        private void ShowConflictBalloon(string title, bool includeHint)
        {
            string body = BuildConflictDescription();
            if (includeHint)
            {
                body = body + (_strings.IsChinese ? "。" : ". ") + _strings.BalloonConflictHint;
            }

            ShowBalloonIfVisible(4500, title, body, ToolTipIcon.Warning);
        }

        private bool TryRegisterHotkeys(bool notifyOnSuccess)
        {
            uint modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT;

            if (!_rectRegistered)
            {
                if (NativeMethods.RegisterHotKey(_window.Handle, NativeMethods.HOTKEY_ID_RECT, modifiers, NativeMethods.VK_4))
                {
                    _rectRegistered = true;
                    _lastRectError = 0;
                }
                else
                {
                    _lastRectError = Marshal.GetLastWin32Error();
                    LogError(string.Format("RegisterHotKey Ctrl+Shift+4 failed with Win32 error {0}", _lastRectError));
                }
            }

            if (!_windowRegistered)
            {
                if (NativeMethods.RegisterHotKey(_window.Handle, NativeMethods.HOTKEY_ID_WINDOW, modifiers, NativeMethods.VK_5))
                {
                    _windowRegistered = true;
                    _lastWindowError = 0;
                }
                else
                {
                    _lastWindowError = Marshal.GetLastWin32Error();
                    LogError(string.Format("RegisterHotKey Ctrl+Shift+5 failed with Win32 error {0}", _lastWindowError));
                }
            }

            UpdateStatusUi();

            bool allRegistered = _rectRegistered && _windowRegistered;
            if (allRegistered && notifyOnSuccess)
            {
                ShowBalloonIfVisible(
                    3000,
                    _strings.BalloonReadyTitle,
                    _strings.BalloonShortcutsBody,
                    ToolTipIcon.Info
                );
            }

            return allRegistered;
        }

        private void UpdateStatusUi()
        {
            if (_rectRegistered && _windowRegistered)
            {
                _statusItem.Text = _strings.StatusRunning;
                SetTrayText(_strings.TrayRunning);
            }
            else if (_rectRegistered)
            {
                _statusItem.Text = string.Format(_strings.StatusRectOkWindowConflictFormat, _strings.FormatErrorShort(_lastWindowError));
                SetTrayText(_strings.TrayConflict5);
            }
            else if (_windowRegistered)
            {
                _statusItem.Text = string.Format(_strings.StatusWindowOkRectConflictFormat, _strings.FormatErrorShort(_lastRectError));
                SetTrayText(_strings.TrayConflict4);
            }
            else
            {
                _statusItem.Text = string.Format(
                    _strings.StatusBothConflictFormat,
                    _strings.FormatErrorShort(_lastRectError),
                    _strings.FormatErrorShort(_lastWindowError)
                );
                SetTrayText(_strings.TrayConflictBoth);
            }
        }

        private string BuildConflictDescription()
        {
            if (!_rectRegistered && !_windowRegistered)
            {
                return string.Format(
                    _strings.ConflictBothFormat,
                    _strings.FormatErrorDetail(_lastRectError),
                    _strings.FormatErrorDetail(_lastWindowError)
                );
            }
            if (!_rectRegistered)
            {
                return string.Format(_strings.ConflictRectOnlyFormat, _strings.FormatErrorDetail(_lastRectError));
            }
            return string.Format(_strings.ConflictWindowOnlyFormat, _strings.FormatErrorDetail(_lastWindowError));
        }

        public void OnPingReceived()
        {
            bool restoredFromHidden = !_trayVisible;
            if (restoredFromHidden)
            {
                SetTrayVisible(true, true);
            }

            if (_rectRegistered && _windowRegistered)
            {
                ShowBalloonIfVisible(
                    2500,
                    restoredFromHidden ? _strings.BalloonTrayRestoredTitle : _strings.BalloonAlreadyRunningTitle,
                    _strings.BalloonShortcutsBody,
                    ToolTipIcon.Info
                );
            }
            else
            {
                if (!TryRegisterHotkeys(true))
                {
                    ShowConflictBalloon(_strings.BalloonConflictTitle, false);
                }
            }
        }

        private void UnregisterAllHotkeysSafe()
        {
            IntPtr hwnd = _window.Handle;
            if (_rectRegistered)
            {
                if (hwnd != IntPtr.Zero)
                {
                    NativeMethods.UnregisterHotKey(hwnd, NativeMethods.HOTKEY_ID_RECT);
                }
                _rectRegistered = false;
            }
            if (_windowRegistered)
            {
                if (hwnd != IntPtr.Zero)
                {
                    NativeMethods.UnregisterHotKey(hwnd, NativeMethods.HOTKEY_ID_WINDOW);
                }
                _windowRegistered = false;
            }
        }

        public void OnHotkeyPressed(int hotkeyId)
        {
            long now = _triggerWatch.ElapsedMilliseconds;
            if (now - _lastTriggerMs < 350)
            {
                return;
            }
            _lastTriggerMs = now;

            if (hotkeyId == NativeMethods.HOTKEY_ID_RECT)
            {
                TriggerScreenshot(UriRectCapture, _strings.LabelRectSnip);
            }
            else if (hotkeyId == NativeMethods.HOTKEY_ID_WINDOW)
            {
                TriggerScreenshot(UriWindowCapture, _strings.LabelWindowSnip);
            }
        }

        private void TriggerScreenshot(string uri, string modeLabel)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(uri)
                {
                    UseShellExecute = true
                };
                using (Process proc = Process.Start(psi))
                {
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to launch " + uri + ": " + ex.Message);
                ShowBalloonIfVisible(
                    3000,
                    string.Format(_strings.BalloonLaunchFailedTitleFormat, modeLabel),
                    string.Format(_strings.BalloonLaunchFailedBodyFormat, ex.Message),
                    ToolTipIcon.Error
                );
            }
        }

        private static string GetStartupShortcutPath()
        {
            string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupDir, ShortcutFileName);
        }

        private static bool IsStartupApprovedEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupApprovedSubKey, false))
                {
                    if (key == null)
                    {
                        return true;
                    }

                    byte[] data = key.GetValue(ShortcutFileName) as byte[];
                    if (data == null || data.Length == 0)
                    {
                        return true;
                    }

                    // Windows stores even first byte (e.g. 0x02/0x06) when enabled, odd (e.g. 0x03) when disabled in Task Manager.
                    return (data[0] & 0x01) == 0;
                }
            }
            catch
            {
                return true;
            }
        }

        private static void UpdateStartupApprovedRegistry(bool enable)
        {
            try
            {
                if (enable)
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(StartupApprovedSubKey))
                    {
                        if (key != null)
                        {
                            byte[] enabledBytes = new byte[12];
                            enabledBytes[0] = 0x02;
                            key.SetValue(ShortcutFileName, enabledBytes, RegistryValueKind.Binary);
                        }
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupApprovedSubKey, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(ShortcutFileName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("UpdateStartupApprovedRegistry warning: " + ex.Message);
            }
        }

        private void RefreshAutostartState()
        {
            try
            {
                string lnkPath = GetStartupShortcutPath();
                bool enabled = false;
                if (File.Exists(lnkPath))
                {
                    string target = ReadShortcutTarget(lnkPath);
                    if (string.Equals(target, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        enabled = IsStartupApprovedEnabled();
                    }
                }
                _autostartItem.Checked = enabled;
            }
            catch
            {
                _autostartItem.Checked = false;
            }
        }

        private void ToggleAutostart()
        {
            try
            {
                string lnkPath = GetStartupShortcutPath();
                if (_autostartItem.Checked)
                {
                    if (File.Exists(lnkPath))
                    {
                        File.Delete(lnkPath);
                    }
                    UpdateStartupApprovedRegistry(false);
                    _autostartItem.Checked = false;
                }
                else
                {
                    WriteShortcut(
                        lnkPath,
                        Application.ExecutablePath,
                        "--startup",
                        Path.GetDirectoryName(Application.ExecutablePath),
                        "MacSnip for Windows (Ctrl+Shift+4/5)"
                    );
                    UpdateStartupApprovedRegistry(true);
                    _autostartItem.Checked = true;
                }
            }
            catch (Exception ex)
            {
                LogError("ToggleAutostart error: " + ex.Message);
                MessageBox.Show(
                    string.Format(_strings.AutostartToggleErrorFormat, ex.Message),
                    "MacSnip",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private static string ReadShortcutTarget(string lnkPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return string.Empty;
            }

            object shell = null;
            object shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                object target = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, null);
                return target as string ?? string.Empty;
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                {
                    Marshal.FinalReleaseComObject(shortcut);
                }
                if (shell != null && Marshal.IsComObject(shell))
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
        }

        private static void WriteShortcut(string lnkPath, string targetPath, string arguments, string workingDir, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell COM component is unavailable.");
            }

            object shell = null;
            object shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                scType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDir });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath + ",0" });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                {
                    Marshal.FinalReleaseComObject(shortcut);
                }
                if (shell != null && Marshal.IsComObject(shell))
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
        }

        public void Shutdown()
        {
            if (_isShuttingDown)
            {
                return;
            }
            _isShuttingDown = true;

            try
            {
                _retryTimer.Stop();
                _retryTimer.Dispose();
            }
            catch
            {
            }

            try
            {
                _trimTimer.Stop();
                _trimTimer.Dispose();
            }
            catch
            {
            }

            UnregisterAllHotkeysSafe();

            DisposeWaitRegistration(_stopWaitHandle, _stopEvent);
            DisposeWaitRegistration(_pingWaitHandle, _pingEvent);
            DisposeWaitRegistration(_showTrayWaitHandle, _showTrayEvent);
            DisposeWaitRegistration(_hideTrayWaitHandle, _hideTrayEvent);

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            if (_menu != null)
            {
                _menu.Dispose();
            }
            if (_boldMenuFont != null)
            {
                _boldMenuFont.Dispose();
            }
            if (_ownedAppIcon != null)
            {
                _ownedAppIcon.Dispose();
            }

            if (_window.Handle != IntPtr.Zero)
            {
                _window.DestroyHandle();
            }

            ExitThread();
        }

        private static void DisposeWaitRegistration(RegisteredWaitHandle waitHandle, EventWaitHandle ev)
        {
            if (waitHandle != null)
            {
                waitHandle.Unregister(null);
            }
            if (ev != null)
            {
                ev.Close();
            }
        }

        public static void LogError(string message)
        {
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} {1}{2}", DateTime.Now, message, Environment.NewLine);

            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            if (!string.IsNullOrEmpty(exeDir) && TryAppendLogLine(Path.Combine(exeDir, "error.log"), line))
            {
                return;
            }

            try
            {
                string localDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MacSnip"
                );
                Directory.CreateDirectory(localDir);
                TryAppendLogLine(Path.Combine(localDir, "error.log"), line);
            }
            catch
            {
            }
        }

        private static bool TryAppendLogLine(string logPath, string line)
        {
            try
            {
                if (File.Exists(logPath))
                {
                    FileInfo info = new FileInfo(logPath);
                    if (info.Length > MaxLogFileBytes)
                    {
                        string backupPath = logPath + ".old";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Move(logPath, backupPath);
                    }
                }

                File.AppendAllText(logPath, line);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class HotkeyMessageWindow : NativeWindow
        {
            private readonly MacSnipAppContext _owner;

            public HotkeyMessageWindow(MacSnipAppContext owner)
            {
                _owner = owner;
                CreateParams cp = new CreateParams
                {
                    Caption = "MacSnip_MessageWindow"
                };
                CreateHandle(cp);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_HOTKEY)
                {
                    int id = m.WParam.ToInt32();
                    if (id == NativeMethods.HOTKEY_ID_RECT || id == NativeMethods.HOTKEY_ID_WINDOW)
                    {
                        _owner.OnHotkeyPressed(id);
                        return;
                    }
                }
                if (m.Msg == NativeMethods.WM_APP_PING)
                {
                    _owner.OnPingReceived();
                    return;
                }
                if (m.Msg == NativeMethods.WM_APP_SHOW_TRAY)
                {
                    _owner.OnShowTrayRequested();
                    return;
                }
                if (m.Msg == NativeMethods.WM_APP_HIDE_TRAY)
                {
                    _owner.OnHideTrayRequested();
                    return;
                }
                if (m.Msg == NativeMethods.WM_APP_EXIT)
                {
                    _owner.Shutdown();
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }

    internal static class Program
    {
        private static Mutex _instanceMutex;

        [STAThread]
        private static int Main(string[] args)
        {
            bool silentStartup = false;
            string languageOverride = null;
            bool? trayOverride = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--stop", StringComparison.OrdinalIgnoreCase))
                {
                    return StopExistingInstance();
                }
                if (string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase))
                {
                    silentStartup = true;
                }
                else if (string.Equals(arg, "--show-tray", StringComparison.OrdinalIgnoreCase))
                {
                    trayOverride = true;
                }
                else if (string.Equals(arg, "--hide-tray", StringComparison.OrdinalIgnoreCase))
                {
                    trayOverride = false;
                }
                else if (string.Equals(arg, "--lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    languageOverride = args[++i];
                }
                else if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                {
                    languageOverride = arg.Substring("--lang=".Length);
                }
            }

            bool createdNew;
            _instanceMutex = new Mutex(true, MacSnipAppContext.MutexName, out createdNew);
            if (!createdNew)
            {
                bool acquired = false;
                try
                {
                    acquired = _instanceMutex.WaitOne(0, false);
                }
                catch (AbandonedMutexException)
                {
                    acquired = true;
                }

                if (!acquired)
                {
                    if (trayOverride.HasValue)
                    {
                        SignalNamedEvent(trayOverride.Value
                            ? MacSnipAppContext.ShowTrayEventName
                            : MacSnipAppContext.HideTrayEventName);
                    }
                    else if (!silentStartup)
                    {
                        SignalNamedEvent(MacSnipAppContext.PingEventName);
                    }

                    _instanceMutex.Close();
                    return 0;
                }
            }

            Application.ThreadException += delegate (object sender, ThreadExceptionEventArgs e)
            {
                if (e != null && e.Exception != null)
                {
                    MacSnipAppContext.LogError("Unhandled UI thread exception: " + e.Exception);
                }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                if (e != null && e.ExceptionObject != null)
                {
                    MacSnipAppContext.LogError("Unhandled domain exception: " + e.ExceptionObject);
                }
            };

            try
            {
                NativeMethods.EnableDpiAwareness();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MacSnipAppContext(silentStartup, languageOverride, trayOverride));
            }
            catch (Exception ex)
            {
                MacSnipAppContext.LogError("Fatal startup/runtime error: " + ex);
                return 1;
            }
            finally
            {
                try
                {
                    _instanceMutex.ReleaseMutex();
                }
                catch
                {
                }
                _instanceMutex.Close();
                GC.KeepAlive(_instanceMutex);
            }

            return 0;
        }

        private static void SignalNamedEvent(string eventName)
        {
            try
            {
                using (EventWaitHandle ev = EventWaitHandle.OpenExisting(eventName))
                {
                    ev.Set();
                }
            }
            catch
            {
            }
        }

        private static int StopExistingInstance()
        {
            try
            {
                using (EventWaitHandle ev = EventWaitHandle.OpenExisting(MacSnipAppContext.StopEventName))
                {
                    ev.Set();
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return 0;
            }

            for (int i = 0; i < 25; i++)
            {
                Thread.Sleep(80);
                bool createdNew;
                using (Mutex m = new Mutex(false, MacSnipAppContext.MutexName, out createdNew))
                {
                    if (createdNew)
                    {
                        return 0;
                    }
                    try
                    {
                        if (m.WaitOne(0, false))
                        {
                            m.ReleaseMutex();
                            return 0;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        return 0;
                    }
                }
            }

            return 0;
        }
    }
}
