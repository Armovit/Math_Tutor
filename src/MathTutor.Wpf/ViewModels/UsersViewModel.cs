using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class UsersViewModel(IUserManagementService userManagementService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    private static readonly UserRoleFilterOption StudentsRoleFilter = new("Только ученики", UserRole.Student);
    private static readonly UserRoleFilterOption AllRolesFilter = new("Все роли", null);
    private static readonly UserRoleFilterOption AdminsRoleFilter = new("Администраторы", UserRole.Admin);
    private static readonly UserStatusFilterOption AnyStatusFilter = new("Любой статус", null);
    private static readonly UserStatusFilterOption ActiveStatusFilter = new("Активные", false);
    private static readonly UserStatusFilterOption BlockedStatusFilter = new("Заблокированные", true);
    private static readonly UserSortOption FullNameSort = new("По фамилии и имени", UserSortBy.FullName);

    [ObservableProperty]
    private string search = string.Empty;

    [ObservableProperty]
    private UserDto? selectedUser;

    [ObservableProperty]
    private UserRoleFilterOption? selectedRoleFilter = StudentsRoleFilter;

    [ObservableProperty]
    private UserStatusFilterOption? selectedStatusFilter = AnyStatusFilter;

    [ObservableProperty]
    private UserSortOption? selectedSortOption = FullNameSort;

    [ObservableProperty]
    private bool sortDescending;

    public ObservableCollection<UserDto> Users { get; } = new();
    public IReadOnlyList<UserRoleFilterOption> RoleFilters { get; } =
    [
        StudentsRoleFilter,
        AllRolesFilter,
        AdminsRoleFilter
    ];
    public IReadOnlyList<UserStatusFilterOption> StatusFilters { get; } =
    [
        AnyStatusFilter,
        ActiveStatusFilter,
        BlockedStatusFilter
    ];
    public IReadOnlyList<UserSortOption> SortOptions { get; } =
    [
        FullNameSort,
        new("По email", UserSortBy.Email),
        new("По роли", UserSortBy.Role),
        new("По дате регистрации", UserSortBy.CreatedAt),
        new("По последнему входу", UserSortBy.LastLogin),
        new("По выполненным заданиям", UserSortBy.CompletedTasks),
        new("По попыткам тестов", UserSortBy.TestAttempts),
        new("По среднему результату тестов", UserSortBy.AverageTestPercent),
        new("По отзывам", UserSortBy.Reviews),
        new("По блокировке", UserSortBy.Blocked)
    ];

    public override string Title => "Пользователи";
    public bool HasUsers => Users.Count > 0;
    public bool CanToggleBlocked => SelectedUser is not null;
    public int StudentsCount => Users.Count(x => x.Role == UserRole.Student);
    public int ActiveCount => Users.Count(x => !x.IsBlocked);
    public int BlockedCount => Users.Count(x => x.IsBlocked);
    public int TotalCompletedTasks => Users.Sum(x => x.CompletedTasks);
    public string UsersSummary => Users.Count == 0
        ? "Пользователи не найдены"
        : $"{Users.Count} найдено · {StudentsCount} учеников · {ActiveCount} активных · {BlockedCount} заблокированных";
    public string SelectedUserTitle => SelectedUser is null ? "Пользователь не выбран" : SelectedUser.FullName;
    public string SelectedUserDetails => SelectedUser is null
        ? "Выберите пользователя в таблице."
        : $"{SelectedUser.Email} · {SelectedUser.StatusText} · {SelectedUser.ActivitySummary}";
    public string ToggleBlockedButtonText => SelectedUser?.IsBlocked == true
        ? "Разблокировать"
        : "Заблокировать";

    partial void OnSelectedUserChanged(UserDto? value)
    {
        OnPropertyChanged(nameof(CanToggleBlocked));
        OnPropertyChanged(nameof(SelectedUserTitle));
        OnPropertyChanged(nameof(SelectedUserDetails));
        OnPropertyChanged(nameof(ToggleBlockedButtonText));
        ToggleBlockedCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        Users.Clear();
        var query = new UserQuery(
            Search,
            SelectedRoleFilter?.Role,
            SelectedStatusFilter?.IsBlocked,
            SelectedSortOption?.SortBy ?? UserSortBy.FullName,
            SortDescending);
        foreach (var user in await userManagementService.SearchUsersDetailedAsync(query)) Users.Add(user);
        OnUsersChanged();
    });

    [RelayCommand]
    private Task ResetFiltersAsync()
    {
        Search = string.Empty;
        SelectedRoleFilter = RoleFilters.First();
        SelectedStatusFilter = StatusFilters.First();
        SelectedSortOption = SortOptions.First();
        SortDescending = false;
        return LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanToggleBlocked))]
    private Task ToggleBlockedAsync() => RunAsync(async () =>
    {
        if (SelectedUser is null || sessionService.CurrentUser is null) return;
        var result = await userManagementService.SetBlockedAsync(SelectedUser.Id, !SelectedUser.IsBlocked, sessionService.CurrentUser.Id);
        StatusMessage = result.Message;
        if (!result.Succeeded) ErrorMessage = result.Message;
        await LoadAsync();
    });

    private void OnUsersChanged()
    {
        OnPropertyChanged(nameof(HasUsers));
        OnPropertyChanged(nameof(StudentsCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(BlockedCount));
        OnPropertyChanged(nameof(TotalCompletedTasks));
        OnPropertyChanged(nameof(UsersSummary));
        ToggleBlockedCommand.NotifyCanExecuteChanged();
    }
}

public sealed record UserRoleFilterOption(string Title, UserRole? Role)
{
    public override string ToString() => Title;
}

public sealed record UserStatusFilterOption(string Title, bool? IsBlocked)
{
    public override string ToString() => Title;
}

public sealed record UserSortOption(string Title, UserSortBy SortBy)
{
    public override string ToString() => Title;
}