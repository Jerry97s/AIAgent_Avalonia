using System.Collections.Specialized;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AiAgentUi.Services;
using AiAgentUi.ViewModels;

namespace AiAgentUi.Views;

public partial class MainView : Window
{
    private ConversationViewModel? _wiredConversation;
    private readonly AgentApiClient _agent;
    private readonly MainViewModel _vm;
    private ActionMemory Memory => ((App)Avalonia.Application.Current!).Memory;

    public MainView()
    {
        InitializeComponent();
        _agent = new AgentApiClient(ResolveAgentBaseUrl());
        _vm = new MainViewModel(_agent, Memory, new FileDialogService());
        DataContext = _vm;

        Memory.LogEvent("main.created");

        Loaded += (_, _) =>
        {
            WireConversationScroll();
            if (MessageBox is { } mb)
                mb.AddHandler(InputElement.KeyDownEvent, MessageBox_KeyDown, RoutingStrategies.Tunnel);
        };
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedConversation))
                WireConversationScroll();
        };

        AddHandler(DragDrop.DragOverEvent, Root_DragOver, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DropEvent, Root_Drop);
    }

    internal const string DefaultAgentBaseUrl = "http://127.0.0.1:8787";

    internal static string ResolveAgentBaseUrl()
    {
        var url = Environment.GetEnvironmentVariable("AGENT_BASE_URL")
            ?? Environment.GetEnvironmentVariable("AI_AGENT_URL");
        if (!string.IsNullOrWhiteSpace(url))
            return url.Trim().TrimEnd('/');
        return DefaultAgentBaseUrl.TrimEnd('/');
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!((App)Avalonia.Application.Current!).ExitRequested)
        {
            e.Cancel = true;
            Hide();
            _vm.Persist();
            Memory.LogEvent("main.hidden");
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.Persist();
        _agent.Dispose();
        Memory.LogEvent("main.closed");
        base.OnClosed(e);
    }

    private void ScrollChatToBottom()
    {
        if (!IsLoaded)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (ChatList.Items.Count > 0)
                ChatList.ScrollIntoView(ChatList.Items[^1]!);
        }, DispatcherPriority.Background);
    }

    private void WireConversationScroll()
    {
        try
        {
            var convo = _vm.SelectedConversation;
            if (convo is null)
                return;

            if (_wiredConversation is not null)
                _wiredConversation.Messages.CollectionChanged -= ConversationMessages_CollectionChanged;

            _wiredConversation = convo;
            _wiredConversation.Messages.CollectionChanged += ConversationMessages_CollectionChanged;
            ScrollChatToBottom();
        }
        catch
        {
            // ignore wiring issues
        }
    }

    private void ConversationMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ScrollChatToBottom();

    private void MessageBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if ((e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            return;

        if (DataContext is MainViewModel vm && vm.SendCommand.CanExecute(null))
        {
            e.Handled = true;
            vm.SendCommand.Execute(null);
        }
    }

    private void Root_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Root_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetFiles() is not { } files)
                return;

            var paths = new List<string>();
            foreach (var file in files)
            {
                string? p = null;
                if (file is IStorageFile sf)
                    p = sf.Path.IsAbsoluteUri ? sf.Path.LocalPath : sf.Path.ToString();
                else if (file.Path.IsAbsoluteUri && file.Path.IsFile)
                    p = file.Path.LocalPath;

                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    paths.Add(p);
            }

            if (paths.Count == 0)
                return;

            _ = _vm.AnalyzeFilesAsync(paths);
        }
        catch (Exception ex)
        {
            Memory.LogEvent("file.drop.error", new { ex.Message });
        }
    }
}
