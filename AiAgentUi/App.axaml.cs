#if WINDOWS
using System.Runtime.InteropServices;
#endif
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AiAgentUi.Services;

namespace AiAgentUi;

public partial class App : Application
{
    private const int HotkeyId = 0xA11;
#if WINDOWS
    private GlobalHotkey? _hotkey;
    private Window? _hotkeyWindow;
    private Win32Properties.CustomWndProcHookCallback? _hotkeyWndProc;
#endif
    private TrayService? _tray;
    private ActionMemory? _memory;
    private bool _exitRequested;

    internal ActionMemory Memory => _memory ?? throw new InvalidOperationException("Memory not initialized.");
    internal bool ExitRequested => _exitRequested;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _memory = new ActionMemory();
        _tray = new TrayService(_memory);
        _tray.OpenRequested += () => Dispatcher.UIThread.Post(ShowOrActivateMainWindow);
        _tray.ExitRequested += () => Dispatcher.UIThread.Post(RequestExit);

        _memory.LogEvent("app.started");

        desktop.MainWindow = new Views.MainView();
        desktop.MainWindow.Show();

        desktop.Exit += (_, _) =>
        {
            _memory?.LogEvent("app.exit");
#if WINDOWS
            if (_hotkeyWindow is not null && _hotkeyWndProc is not null)
                Win32Properties.RemoveWndProcHookCallback(_hotkeyWindow, _hotkeyWndProc);

            _hotkey?.Dispose();
            try
            {
                _hotkeyWindow?.Close();
            }
            catch
            {
                // ignore
            }
#endif
            _tray?.Dispose();
        };

        Dispatcher.UIThread.Post(BringMainWindowToForeground, DispatcherPriority.Background);
#if WINDOWS
        Dispatcher.UIThread.Post(InitializeGlobalHotkeyShell, DispatcherPriority.Background);
        const string startupHint =
            "창을 닫으면 트레이로 숨깁니다. Ctrl+F12로 다시 열 수 있어요.";
#else
        const string startupHint =
            "창을 닫으면 트레이로 숨깁니다. 트레이 아이콘에서 다시 열 수 있어요.";
#endif
        Dispatcher.UIThread.Post(
            () => ShowStartupHintNearTray(desktop.MainWindow, startupHint),
            DispatcherPriority.Background);

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowStartupHintNearTray(Window? anchor, string body)
    {
        ShowTrayHintWindow(
            anchor,
            "AiAgentUi",
            "AI Agent UI",
            body);
    }

    private void ShowTrayHintWindow(Window? anchor, string titleBar, string headline, string body, int dismissMs = 3200)
    {
        WindowIcon? ico;
        try
        {
            using var s = Avalonia.Platform.AssetLoader.Open(new Uri("avares://AiAgentUi/Assets/app.ico"));
            ico = new WindowIcon(s);
        }
        catch
        {
            ico = null;
        }

        var win = new Window
        {
            Title = titleBar,
            Icon = ico,
            Width = 328,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false,
            Topmost = true,
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Inter, Noto Sans, sans-serif"),
            Background = Brushes.White,
        };

        var root = new StackPanel { Margin = new Thickness(14, 11, 14, 13) };
        root.Children.Add(new TextBlock
        {
            Text = headline,
            FontSize = 13.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27)),
            TextWrapping = TextWrapping.Wrap,
        });
        root.Children.Add(new TextBlock
        {
            Text = body,
            Margin = new Thickness(0, 8, 0, 0),
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(113, 113, 122)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18,
        });
        win.Content = root;

        win.Loaded += (_, _) =>
        {
            win.InvalidateMeasure();
            win.InvalidateArrange();
            if (anchor?.Screens is not { } screens)
                return;
            var screen = screens.ScreenFromWindow(anchor) ?? screens.Primary;
            if (screen is null)
                return;
            var wa = screen.WorkingArea;
            var w = win.Bounds.Width > 0 ? win.Bounds.Width : win.Width;
            var h = win.Bounds.Height > 0 ? win.Bounds.Height : win.DesiredSize.Height;
            win.Position = new PixelPoint(
                wa.X + wa.Width - (int)w - 16,
                wa.Y + wa.Height - (int)h - 12);
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(dismissMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                win.Close();
            }
            catch
            {
                // ignore
            }
        };

        timer.Start();
        win.Show();
    }

    private void BringMainWindowToForeground()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime d || d.MainWindow is null)
            return;

        var mw = d.MainWindow;
        mw.ShowInTaskbar = true;
        mw.WindowState = WindowState.Normal;
        mw.Show();
        mw.Topmost = true;
        mw.Activate();
        mw.Topmost = false;
#if WINDOWS
        TryWin32Foreground(mw);
#endif
        _memory?.LogEvent("main.foreground");
    }

#if WINDOWS
    private static void TryWin32Foreground(Window w)
    {
        try
        {
            var hwnd = w.TryGetPlatformHandle()?.Handle ?? default;
            if (hwnd == default)
                return;
            ShowWindow(hwnd, SW_SHOW);
            if (IsIconic(hwnd))
                ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);
        }
        catch
        {
            // ignore
        }
    }

    private void InitializeGlobalHotkeyShell()
    {
        if (_hotkeyWindow is not null)
            return;

        WindowIcon? hotkeyIco = null;
        try
        {
            using var s = Avalonia.Platform.AssetLoader.Open(new Uri("avares://AiAgentUi/Assets/app.ico"));
            hotkeyIco = new WindowIcon(s);
        }
        catch
        {
            // ignore
        }

        _hotkeyWindow = new Window
        {
            Width = 1,
            Height = 1,
            Position = new PixelPoint(-10_000, -10_000),
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            Opacity = 1,
            Icon = hotkeyIco,
            CanResize = false,
        };

        _hotkeyWindow.Opened += (_, _) =>
        {
            var hwnd = _hotkeyWindow!.TryGetPlatformHandle()?.Handle ?? default;
            if (hwnd == default)
                return;

            _hotkeyWndProc = HotkeyWndProc;

            Win32Properties.AddWndProcHookCallback(_hotkeyWindow, _hotkeyWndProc);

            _hotkey = new GlobalHotkey(hwnd, HotkeyId);
            var ok = _hotkey.Register(
                GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.NoRepeat,
                VkF12);

            _memory?.LogEvent("hotkey.register", new { ok, keys = "Ctrl+F12" });

            if (!ok)
                Dispatcher.UIThread.Post(() =>
                    ShowTrayHintWindow(
                        Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null,
                        "AiAgentUi",
                        "AI Agent UI",
                        "Ctrl+F12 전역 단축키 등록에 실패했습니다. (이미 사용 중일 수 있어요)"));
        };

        _hotkeyWindow.Show();
    }

    private nint HotkeyWndProc(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        _hotkey?.ProcessMessage(msg, wParam);
        if (msg == 0x0312 && (int)wParam == HotkeyId)
        {
            handled = true;
            Dispatcher.UIThread.Post(ShowOrActivateMainWindow);
        }

        return IntPtr.Zero;
    }

    private const uint VkF12 = 0x7B;

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);
#endif

    private void RequestExit()
    {
        _exitRequested = true;
        _memory?.LogEvent("app.exit.requested");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            d.Shutdown();
    }

    private void ShowOrActivateMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is null)
            desktop.MainWindow = new Views.MainView();

        var w = desktop.MainWindow;
        w.ShowInTaskbar = true;

        if (!w.IsVisible)
            w.Show();

        if (w.WindowState == WindowState.Minimized)
            w.WindowState = WindowState.Normal;

        w.Topmost = true;
        w.Activate();
        w.Topmost = false;
#if WINDOWS
        TryWin32Foreground(w);
#endif
        w.Focus();
        _memory?.LogEvent("main.show");
    }
}
