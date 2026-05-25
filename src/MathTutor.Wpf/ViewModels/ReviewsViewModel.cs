using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class ReviewsViewModel(IReviewService reviewService, IMaterialService materialService, ITaskService taskService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    private readonly List<ReviewDto> allReviews = new();
    private readonly SemaphoreSlim dataOperationLock = new(1, 1);
    private bool suppressMaterialReload;

    [ObservableProperty]
    private MaterialDto? selectedMaterial;

    [ObservableProperty]
    private TaskDto? selectedTask;

    [ObservableProperty]
    private string? selectedTopicFilter;

    [ObservableProperty]
    private MaterialDto? selectedMaterialFilter;

    [ObservableProperty]
    private int difficultyRating = 3;

    [ObservableProperty]
    private int usefulnessRating = 5;

    [ObservableProperty]
    private string comment = string.Empty;

    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public ObservableCollection<TaskDto> Tasks { get; } = new();
    public ObservableCollection<string> TopicFilters { get; } = new();
    public ObservableCollection<ReviewDto> Reviews { get; } = new();
    public override string Title => "Отзывы";
    public bool CanAddReview => sessionService.CurrentUser?.Role == UserRole.Student;
    public bool IsAdminMode => sessionService.CurrentUser?.Role == UserRole.Admin;
    public bool HasReviews => Reviews.Count > 0;
    public bool HasSelectedMaterial => SelectedMaterial is not null;
    public bool HasSelectedTask => SelectedTask is not null;
    public string ReviewTargetHint => SelectedTask is not null
        ? $"Отзыв будет прикреплён к заданию: {SelectedTask.Title}"
        : SelectedMaterial is not null
            ? $"Отзыв будет прикреплён к материалу: {SelectedMaterial.Title}"
            : "Выберите тему, тест или материал для отзыва.";
    public string ReviewsSummary => allReviews.Count == 0
        ? "Пока нет отзывов"
        : $"{Reviews.Count} из {allReviews.Count} отзывов";

    public Task LoadAsync() => RunAsync(async () =>
    {
        Materials.Clear();
        TopicFilters.Clear();
        TopicFilters.Add("Все темы");

        var materials = await materialService.GetMaterialsAsync(new MaterialQuery(PublishedOnly: true, SortBy: "Topic"));
        foreach (var material in materials.OrderBy(x => x.Topic).ThenBy(x => x.Title)) Materials.Add(material);
        foreach (var topic in materials.Select(x => NormalizeTopic(x.Topic)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) TopicFilters.Add(topic);

        SelectedTopicFilter = TopicFilters.FirstOrDefault();
        suppressMaterialReload = true;
        SelectedMaterial ??= Materials.FirstOrDefault();
        suppressMaterialReload = false;
        OnPropertyChanged(nameof(CanAddReview));
        OnPropertyChanged(nameof(IsAdminMode));
        OnPropertyChanged(nameof(HasSelectedMaterial));
        await LoadTasksForSelectedMaterialAsync();
        await LoadReviewsCoreAsync();
    });

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        SelectedTask = null;
        OnPropertyChanged(nameof(HasSelectedMaterial));
        OnPropertyChanged(nameof(ReviewTargetHint));
        if (suppressMaterialReload) return;
        _ = LoadTasksForSelectedMaterialAsync();
    }

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedTask));
        OnPropertyChanged(nameof(ReviewTargetHint));
    }

    partial void OnSelectedTopicFilterChanged(string? value)
    {
        ApplyReviewFilters();
    }

    partial void OnSelectedMaterialFilterChanged(MaterialDto? value)
    {
        ApplyReviewFilters();
    }

    [RelayCommand]
    private Task AddReviewAsync() => RunAsync(async () =>
    {
        if (sessionService.CurrentUser is null || SelectedMaterial is null) return;
        await dataOperationLock.WaitAsync();
        OperationResult<ReviewDto> result;
        try
        {
            result = await reviewService.AddReviewAsync(new ReviewEditDto(sessionService.CurrentUser.Id, SelectedMaterial.Id, SelectedTask?.Id, DifficultyRating, UsefulnessRating, Comment));
        }
        finally
        {
            dataOperationLock.Release();
        }

        StatusMessage = result.Message;
        if (!result.Succeeded) ErrorMessage = result.Message;
        if (result.Succeeded)
        {
            Comment = string.Empty;
            await LoadReviewsCoreAsync();
        }
    });

    [RelayCommand]
    private Task RefreshReviewsAsync() => RunAsync(LoadReviewsCoreAsync);

    [RelayCommand]
    private void ResetReviewFilters()
    {
        SelectedTopicFilter = TopicFilters.FirstOrDefault();
        SelectedMaterialFilter = null;
        ApplyReviewFilters();
    }

    [RelayCommand]
    private void ClearTaskSelection()
    {
        SelectedTask = null;
    }

    private async Task LoadTasksForSelectedMaterialAsync()
    {
        await dataOperationLock.WaitAsync();
        try
        {
            Tasks.Clear();
            if (SelectedMaterial is not null)
            {
                foreach (var task in await taskService.GetTasksAsync(SelectedMaterial.Id)) Tasks.Add(task);
            }

            OnPropertyChanged(nameof(ReviewTargetHint));
        }
        finally
        {
            dataOperationLock.Release();
        }
    }

    private async Task LoadReviewsCoreAsync()
    {
        await dataOperationLock.WaitAsync();
        try
        {
            allReviews.Clear();
            allReviews.AddRange(await reviewService.GetReviewsAsync());
            ApplyReviewFilters();
        }
        finally
        {
            dataOperationLock.Release();
        }
    }

    private void ApplyReviewFilters()
    {
        Reviews.Clear();
        var query = allReviews.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedTopicFilter) && SelectedTopicFilter != "Все темы")
        {
            query = query.Where(x => string.Equals(NormalizeTopic(x.MaterialTopic), SelectedTopicFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedMaterialFilter is not null)
        {
            query = query.Where(x => x.EducationalMaterialId == SelectedMaterialFilter.Id);
        }

        foreach (var review in query) Reviews.Add(review);

        OnPropertyChanged(nameof(HasReviews));
        OnPropertyChanged(nameof(ReviewsSummary));
    }

    private static string NormalizeTopic(string? topic) => string.IsNullOrWhiteSpace(topic) ? "Без темы" : topic.Trim();
}
