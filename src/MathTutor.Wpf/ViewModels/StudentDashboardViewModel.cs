using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class StudentDashboardViewModel(IMaterialService materialService, IProgressService progressService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private ProgressSummaryDto? progress;

    public ObservableCollection<MaterialDto> RecentMaterials { get; } = new();
    public ObservableCollection<ISeries> TopicSeries { get; } = new();
    public Axis[] TopicXAxes { get; private set; } = [new Axis { Labels = [] }];
    public Axis[] TopicYAxes { get; } =
    [
        new Axis
        {
            MinLimit = 0,
            MaxLimit = 100,
            MinStep = 25,
            Labeler = value => $"{value:0}%"
        }
    ];
    public override string Title => "Панель ученика";

    public Task LoadAsync() => RunAsync(async () =>
    {
        RecentMaterials.Clear();
        foreach (var item in (await materialService.GetMaterialsAsync(new MaterialQuery(PublishedOnly: true, SortBy: "Date"))).Take(4))
        {
            RecentMaterials.Add(item);
        }

        if (sessionService.CurrentUser is not null)
        {
            Progress = await progressService.GetStudentProgressAsync(sessionService.CurrentUser.Id);
            BuildTopicChart();
        }
    });

    private void BuildTopicChart()
    {
        TopicSeries.Clear();
        var topics = Progress?.TopicProgress
            .Where(x => x.AttemptCount > 0 || x.CompletedTasks > 0)
            .OrderBy(x => x.AveragePercent)
            .Take(8)
            .ToList() ?? [];

        TopicXAxes =
        [
            new Axis
            {
                Labels = topics.Select(x => ShortenTopic(x.Topic)).ToArray(),
                LabelsRotation = 0,
                TextSize = 11
            }
        ];
        TopicSeries.Add(new ColumnSeries<decimal>
        {
            Name = "Средний результат",
            Values = topics.Select(x => x.AveragePercent).ToArray(),
            MaxBarWidth = 44,
            Padding = 10
        });
        OnPropertyChanged(nameof(TopicXAxes));
    }

    private static string ShortenTopic(string topic)
    {
        const int maxLength = 16;
        return topic.Length <= maxLength ? topic : $"{topic[..maxLength]}...";
    }
}
