namespace AiAgentUi.Services;

public interface IFileDialogService
{
    Task<(bool ok, string path)> TryPickTextFileAsync();
}
