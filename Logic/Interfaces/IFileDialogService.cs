namespace VideoTranslator.Interfaces;

public interface IFileDialogService
{
    string? OpenVideoFile(string? defaultPath = null);
    string? OpenFile(string filter, string? defaultPath = null);
    string? SaveFile(string filter, string? defaultPath = null, string? defaultFileName = null);
}
