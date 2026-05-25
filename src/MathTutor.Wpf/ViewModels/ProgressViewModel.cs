using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;
using MathTutor.Wpf.Services;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class ProgressViewModel(
    IProgressService progressService,
    ITestService testService,
    IMaterialService materialService,
    IReportExportService reportExportService,
    ISessionService sessionService,
    IFileDialogService fileDialogService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private ProgressSummaryDto? progress;

    [ObservableProperty]
    private MaterialDto? selectedAttemptMaterial;

    [ObservableProperty]
    private string selectedAttemptTopic = "Все темы";

    public ObservableCollection<TestAttemptDto> Attempts { get; } = new();
    public ObservableCollection<MaterialDto> TestMaterials { get; } = new();
    public ObservableCollection<string> AttemptTopics { get; } = new(["Все темы"]);
    public override string Title => "Прогресс";

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        if (sessionService.CurrentUser is null) return;
        Progress = await progressService.GetStudentProgressAsync(sessionService.CurrentUser.Id);
        TestMaterials.Clear();
        foreach (var material in await materialService.GetMaterialsAsync(new MaterialQuery(Type: MaterialType.Test, PublishedOnly: true, SortBy: "Topic")))
        {
            TestMaterials.Add(material);
        }

        AttemptTopics.Clear();
        AttemptTopics.Add("Все темы");
        foreach (var topic in TestMaterials.Select(x => string.IsNullOrWhiteSpace(x.Topic) ? "Без темы" : x.Topic).Distinct().OrderBy(x => x))
        {
            AttemptTopics.Add(topic);
        }

        await LoadAttemptHistoryAsync();
    });

    [RelayCommand]
    private Task ApplyAttemptFiltersAsync() => LoadAttemptHistoryAsync();

    [RelayCommand]
    private void ResetAttemptFilters()
    {
        SelectedAttemptMaterial = null;
        SelectedAttemptTopic = "Все темы";
        _ = LoadAttemptHistoryAsync();
    }

    [RelayCommand]
    private Task ExportProgressAsync() => RunAsync(async () =>
    {
        if (Progress is null) return;
        var path = fileDialogService.GetSaveFilePath("Экспорт прогресса", "Excel workbook (*.xlsx)|*.xlsx", "student-progress.xlsx");
        if (path is null) return;

        await using var stream = File.Create(path);
        await reportExportService.ExportStudentProgressAsync(Progress, stream);
        StatusMessage = $"Отчёт сохранён: {path}";
    });

    private async Task LoadAttemptHistoryAsync()
    {
        if (sessionService.CurrentUser is null) return;
        Attempts.Clear();
        var topic = SelectedAttemptTopic == "Все темы" ? null : SelectedAttemptTopic;
        var request = new TestAttemptQuery(sessionService.CurrentUser.Id, SelectedAttemptMaterial?.Id, topic);
        foreach (var attempt in await testService.GetAttemptsAsync(request))
        {
            Attempts.Add(attempt);
        }
    }
}
