using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Wpf.Services;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class StatisticsViewModel(IProgressService progressService, IReportExportService reportExportService, IFileDialogService fileDialogService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private AdminStatisticsDto? statistics;

    public override string Title => "Статистика";

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        Statistics = await progressService.GetAdminStatisticsAsync();
    });

    [RelayCommand]
    private Task ExportStatisticsAsync() => RunAsync(async () =>
    {
        if (Statistics is null) return;
        var path = fileDialogService.GetSaveFilePath("Экспорт статистики", "Excel workbook (*.xlsx)|*.xlsx", "admin-statistics.xlsx");
        if (path is null) return;

        await using var stream = File.Create(path);
        await reportExportService.ExportAdminStatisticsAsync(Statistics, stream);
        StatusMessage = $"Отчёт сохранён: {path}";
    });
}
