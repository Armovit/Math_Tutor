using System.Diagnostics;
using MathTutor.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MathTutor.Wpf.Navigation;

public sealed class NavigationService(IServiceScopeFactory scopeFactory) : INavigationService, IDisposable
{
    private IServiceScope? viewModelScope;

    public ViewModelBase? CurrentViewModel { get; private set; }
    public event EventHandler? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        viewModelScope?.Dispose();
        viewModelScope = scopeFactory.CreateScope();
        CurrentViewModel = viewModelScope.ServiceProvider.GetRequiredService<TViewModel>();
        CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);
        if (CurrentViewModel is ILoadableViewModel loadable)
        {
            _ = LoadAsync(loadable);
        }
    }

    public void Dispose() => viewModelScope?.Dispose();

    private async Task LoadAsync(ILoadableViewModel loadable)
    {
        try
        {
            await loadable.LoadAsync();
            CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }
    }
}
