using MathTutor.Wpf.ViewModels;

namespace MathTutor.Wpf.Navigation;

public static class NavigationServiceExtensions
{
    public static void NavigateTo(this INavigationService navigationService, Type viewModelType)
    {
        var method = typeof(INavigationService).GetMethod(nameof(INavigationService.NavigateTo))!.MakeGenericMethod(viewModelType);
        method.Invoke(navigationService, null);
    }
}
