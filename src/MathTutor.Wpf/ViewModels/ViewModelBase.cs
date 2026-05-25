using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MathTutor.Wpf.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public virtual string Title => string.Empty;

    protected async Task RunAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            await action();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            ErrorMessage = $"Не удалось выполнить действие: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public interface ILoadableViewModel
{
    Task LoadAsync();
}
