namespace MathTutor.Wpf.Services;

public interface IFileDialogService
{
    string? GetSaveFilePath(string title, string filter, string defaultFileName);
}
