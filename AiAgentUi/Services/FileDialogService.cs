using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AiAgentUi.Services;

public sealed class FileDialogService : IFileDialogService
{
    public async Task<(bool ok, string path)> TryPickTextFileAsync()
    {
        var life = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var window = life?.MainWindow;
        if (window is null)
            return (false, "");

        var results = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "업로드할 로그/텍스트 파일 선택",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("텍스트/로그")
                {
                    Patterns = ["*.txt", "*.log", "*.csv", "*.json", "*.xml", "*.yml", "*.yaml", "*.md"],
                },
                FilePickerFileTypes.All,
            ],
        }).ConfigureAwait(true);

        if (results is null || results.Count == 0)
            return (false, "");

        var path = results[0].TryGetLocalPath();
        return string.IsNullOrEmpty(path) ? (false, "") : (true, path);
    }
}
