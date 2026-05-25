using MathTutor.Wpf.ViewModels;

namespace MathTutor.Wpf.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    event EventHandler? CurrentViewModelChanged;
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}
