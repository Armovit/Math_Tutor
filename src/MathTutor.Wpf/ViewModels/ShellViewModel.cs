using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Domain.Enums;
using MathTutor.Wpf.Navigation;
using MathTutor.Wpf.Services;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly ISessionService sessionService;
    private readonly INavigationService navigationService;
    private readonly IThemeService themeService;

    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    [ObservableProperty]
    private string currentUserName = string.Empty;

    [ObservableProperty]
    private string currentUserRole = string.Empty;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private bool isSettingsOpen;

    [ObservableProperty]
    private string currentThemeName = "Светлая";

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new();
    public override string Title => CurrentViewModel?.Title ?? "Математический репетиторий";

    public ShellViewModel(ISessionService sessionService, INavigationService navigationService, IThemeService themeService)
    {
        this.sessionService = sessionService;
        this.navigationService = navigationService;
        this.themeService = themeService;
        CurrentThemeName = this.themeService.CurrentThemeName;
        this.navigationService.CurrentViewModelChanged += (_, _) =>
        {
            CurrentViewModel = this.navigationService.CurrentViewModel;
            OnPropertyChanged(nameof(Title));
            RefreshNavigationItems();
        };
        this.sessionService.SessionChanged += (_, _) => RefreshSession();
        RefreshSession();
        this.navigationService.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        sessionService.Clear();
        navigationService.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        themeService.ApplyLightTheme();
        CurrentThemeName = themeService.CurrentThemeName;
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        themeService.ApplyDarkTheme();
        CurrentThemeName = themeService.CurrentThemeName;
        IsSettingsOpen = false;
    }

    private void RefreshSession()
    {
        var current = sessionService.CurrentUser;
        IsAuthenticated = current is not null;
        CurrentUserName = current?.FullName ?? string.Empty;
        CurrentUserRole = current?.Role.ToString() ?? string.Empty;
        RefreshNavigationItems();
    }

    private void RefreshNavigationItems()
    {
        NavigationItems.Clear();
        var current = sessionService.CurrentUser;
        if (current is null) return;

        if (current.Role == UserRole.Admin)
        {
            Add("Dashboard", "⌂", new RelayCommand(() => navigationService.NavigateTo<AdminDashboardViewModel>()));
            Add("Материалы", "□", new RelayCommand(() => navigationService.NavigateTo<MaterialsManagementViewModel>()));
            Add("Задачи", "✓", new RelayCommand(() => navigationService.NavigateTo<TaskManagementViewModel>()));
            Add("Пользователи", "◎", new RelayCommand(() => navigationService.NavigateTo<UsersViewModel>()));
            Add("Статистика", "↗", new RelayCommand(() => navigationService.NavigateTo<StatisticsViewModel>()));
            Add("Уведомления", "•", new RelayCommand(() => navigationService.NavigateTo<NotificationsViewModel>()));
        }
        else
        {
            Add("Dashboard", "⌂", new RelayCommand(() => navigationService.NavigateTo<StudentDashboardViewModel>()));
            Add("Теория", "◫", new RelayCommand(() => navigationService.NavigateTo<TheoryViewModel>()));
            Add("Тесты", "✓", new RelayCommand(() => navigationService.NavigateTo<StudentMaterialsViewModel>()));
            Add("Прогресс", "↗", new RelayCommand(() => navigationService.NavigateTo<ProgressViewModel>()));
            Add("Отзывы", "☆", new RelayCommand(() => navigationService.NavigateTo<ReviewsViewModel>()));
            Add("Уведомления", "•", new RelayCommand(() => navigationService.NavigateTo<NotificationsViewModel>()));
        }
    }

    private void Add(string title, string icon, IRelayCommand command)
    {
        var isActive = CurrentViewModel?.Title == title || (title == "Dashboard" && CurrentViewModel?.Title.Contains("панель", StringComparison.OrdinalIgnoreCase) == true);
        NavigationItems.Add(new NavigationItemViewModel(title, icon, command, isActive));
    }
}
