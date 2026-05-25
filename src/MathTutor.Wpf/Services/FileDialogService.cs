using Microsoft.Win32;

namespace MathTutor.Wpf.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? GetSaveFilePath(string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
