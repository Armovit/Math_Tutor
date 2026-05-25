using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;
using MathTutor.Wpf.Navigation;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class LoginViewModel(IAuthService authService, ISessionService sessionService, INavigationService navigationService) : ViewModelBase
{
    [ObservableProperty]
    private string email = "admin@mathtutor.local";

    [ObservableProperty]
    private string password = "Admin123!";

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isRegisterMode;

    [ObservableProperty]
    private bool isPasswordVisible;

    [ObservableProperty]
    private bool isConfirmPasswordVisible;

    public override string Title => IsRegisterMode ? "Регистрация" : "Вход";

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        IsPasswordVisible = false;
        IsConfirmPasswordVisible = false;
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        await RunAsync(async () =>
        {
            var result = IsRegisterMode
                ? await authService.RegisterAsync(new RegisterRequest(FirstName, LastName, Email, Password, ConfirmPassword))
                : await authService.LoginAsync(new LoginRequest(Email, Password));

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Message;
                return;
            }

            sessionService.SetCurrentUser(result.Value);
            navigationService.NavigateTo(result.Value.Role == UserRole.Admin ? typeof(AdminDashboardViewModel) : typeof(StudentDashboardViewModel));
        });
    }
}
