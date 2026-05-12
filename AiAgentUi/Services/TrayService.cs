using Avalonia.Controls;
using Avalonia.Platform;

namespace AiAgentUi.Services;

public sealed class TrayService : IDisposable
{
    private readonly TrayIcon _tray;
    private readonly ActionMemory _memory;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    public TrayService(ActionMemory memory)
    {
        _memory = memory;

        WindowIcon? icon = null;
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://AiAgentUi/Assets/app.ico"));
            icon = new WindowIcon(s);
        }
        catch
        {
            try
            {
                using var s = AssetLoader.Open(new Uri("avares://AiAgentUi/Assets/logo.png"));
                icon = new WindowIcon(s);
            }
            catch
            {
                // ignore
            }
        }

        var menu = new NativeMenu();
        var openItem = new NativeMenuItem("열기 (Ctrl+F12)");
        openItem.Click += (_, _) => OpenRequested?.Invoke();
        menu.Items.Add(openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        var exitItem = new NativeMenuItem("종료");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        _tray = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "AI Agent UI (Ctrl+F12)",
            Menu = menu,
            IsVisible = true,
        };

        _tray.Clicked += (_, _) => OpenRequested?.Invoke();

        _memory.LogEvent("tray.created");
    }

    public void Dispose()
    {
        _memory.LogEvent("tray.disposed");
        _tray.IsVisible = false;
        _tray.Menu = null;
        _tray.Dispose();
    }
}
