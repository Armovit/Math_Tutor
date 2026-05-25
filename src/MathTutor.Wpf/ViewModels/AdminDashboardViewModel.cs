using CommunityToolkit.Mvvm.ComponentModel;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class AdminDashboardViewModel(IProgressService progressService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private AdminStatisticsDto? statistics;

    public override string Title => "Панель администратора";

    public Task LoadAsync() => RunAsync(async () =>
    {
        Statistics = await progressService.GetAdminStatisticsAsync();
        StatusMessage = "Сводка обновлена.";
    });
}
