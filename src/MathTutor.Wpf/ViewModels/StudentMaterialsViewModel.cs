using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;
using MathTutor.Wpf.Views;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class StudentMaterialsViewModel(IMaterialService materialService, ITaskService taskService, ITestService testService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    private readonly Dictionary<int, string> testAnswers = new();
    private DateTime testStartedAtUtc;

    [ObservableProperty]
    private string search = string.Empty;

    [ObservableProperty]
    private string topicFilter = string.Empty;

    [ObservableProperty]
    private string sectionFilter = string.Empty;

    [ObservableProperty]
    private DifficultyLevel? difficultyFilter;

    [ObservableProperty]
    private MaterialDto? selectedMaterial;

    [ObservableProperty]
    private TaskDto? selectedTask;

    [ObservableProperty]
    private string answer = string.Empty;

    [ObservableProperty]
    private bool testStarted;

    [ObservableProperty]
    private bool testCompleted;

    [ObservableProperty]
    private int currentTaskIndex;

    [ObservableProperty]
    private AnswerOptionSelectionViewModel? selectedAnswerOption;

    [ObservableProperty]
    private TestAttemptDto? testResult;

    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public ObservableCollection<TestTopicViewModel> Topics { get; } = new();
    public ObservableCollection<TaskDto> Tasks { get; } = new();
    public ObservableCollection<AnswerOptionSelectionViewModel> AnswerOptions { get; } = new();
    public IEnumerable<DifficultyLevel?> DifficultyFilters { get; } = new DifficultyLevel?[] { null }.Concat(Enum.GetValues<DifficultyLevel>().Cast<DifficultyLevel?>());
    public override string Title => "Тесты";
    [ObservableProperty]
    private TestTopicViewModel? selectedTopic;

    public bool IsSelectedMaterialTest => SelectedMaterial?.Type == MaterialType.Test;
    public bool IsPracticeMode => !IsSelectedMaterialTest;
    public bool CanStartTest => IsSelectedMaterialTest && Tasks.Count > 0 && !TestStarted;
    public bool HasTasks => Tasks.Count > 0;
    public TaskDto? CurrentTask => IsSelectedMaterialTest ? Tasks.ElementAtOrDefault(CurrentTaskIndex) : SelectedTask;
    public string QuestionProgressText => Tasks.Count == 0 ? "Нет вопросов" : $"Вопрос {CurrentTaskIndex + 1} из {Tasks.Count}";
    public decimal TestMaxScore => Tasks.Sum(x => x.MaxScore);
    public bool IsTextAnswer => CurrentTask?.AnswerType is AnswerType.Text or AnswerType.Number or AnswerType.ManualReview;
    public bool IsSingleChoiceAnswer => CurrentTask?.AnswerType == AnswerType.SingleChoice;
    public bool IsMultipleChoiceAnswer => CurrentTask?.AnswerType == AnswerType.MultipleChoice;
    public bool CanGoPrevious => TestStarted && CurrentTaskIndex > 0;
    public bool CanGoNext => TestStarted && CurrentTaskIndex < Tasks.Count - 1;
    public bool CanFinishTest => TestStarted && Tasks.Count > 0;

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        _ = LoadTasksAsync();
    }

    partial void OnSelectedTopicChanged(TestTopicViewModel? value)
    {
        SelectedMaterial = value?.Tests.FirstOrDefault();
        OnPropertyChanged(nameof(HasSelectedTopic));
    }

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        if (!IsSelectedMaterialTest) LoadAnswerEditor(value, string.Empty);
    }

    partial void OnSelectedAnswerOptionChanged(AnswerOptionSelectionViewModel? value)
    {
        if (IsSingleChoiceAnswer) Answer = value?.Text ?? string.Empty;
    }

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        Materials.Clear();
        Topics.Clear();
        var tests = await materialService.GetMaterialsAsync(new MaterialQuery(Search: Search, Topic: TopicFilter, Section: SectionFilter, Type: MaterialType.Test, Difficulty: DifficultyFilter, PublishedOnly: true, SortBy: "Topic"));
        foreach (var item in tests) Materials.Add(item);
        foreach (var group in tests.GroupBy(x => NormalizeTopicKey(x.Topic), StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var sections = group
                .GroupBy(x => NormalizeSectionKey(x.Section), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new MaterialSectionViewModel(DisplaySectionName(x), x.OrderBy(m => m.Title).ToList()))
                .ToList();
            Topics.Add(new TestTopicViewModel(DisplayTopicName(group), sections));
        }

        SelectedTopic = Topics.FirstOrDefault();
        OnPropertyChanged(nameof(CatalogSummary));
        OnPropertyChanged(nameof(HasCatalogItems));
    });

    [RelayCommand]
    private void ResetFilters()
    {
        Search = string.Empty;
        TopicFilter = string.Empty;
        SectionFilter = string.Empty;
        DifficultyFilter = null;
        _ = LoadAsync();
    }

    private Task LoadTasksAsync() => RunAsync(async () =>
    {
        ResetTestState();
        Tasks.Clear();
        if (SelectedMaterial is null) return;
        foreach (var task in await taskService.GetTasksAsync(SelectedMaterial.Id)) Tasks.Add(task);
        SelectedTask = Tasks.FirstOrDefault();
        LoadAnswerEditor(CurrentTask, string.Empty);
        OnMaterialStateChanged();
    });

    [RelayCommand]
    private Task SubmitAsync() => RunAsync(async () =>
    {
        if (SelectedTask is null || sessionService.CurrentUser is null) return;
        var result = await taskService.SubmitAnswerAsync(new SubmitAnswerRequest(sessionService.CurrentUser.Id, SelectedTask.Id, Answer));
        StatusMessage = result.Value?.Feedback ?? result.Message;
        if (!result.Succeeded) ErrorMessage = result.Message;
        Answer = string.Empty;
    });

    [RelayCommand]
    private void StartTest()
    {
        if (!IsSelectedMaterialTest || Tasks.Count == 0) return;
        testAnswers.Clear();
        testStartedAtUtc = DateTime.UtcNow;
        CurrentTaskIndex = 0;
        TestStarted = true;
        TestCompleted = false;
        TestResult = null;
        StatusMessage = string.Empty;
        LoadAnswerEditor(CurrentTask, string.Empty);
        OnMaterialStateChanged();
    }

    [RelayCommand]
    private void PreviousQuestion()
    {
        if (!CanGoPrevious) return;
        SaveCurrentAnswer();
        CurrentTaskIndex--;
        LoadAnswerEditor(CurrentTask, testAnswers.GetValueOrDefault(CurrentTask?.Id ?? 0, string.Empty));
        OnQuestionStateChanged();
    }

    [RelayCommand]
    private void NextQuestion()
    {
        if (!CanGoNext) return;
        SaveCurrentAnswer();
        CurrentTaskIndex++;
        LoadAnswerEditor(CurrentTask, testAnswers.GetValueOrDefault(CurrentTask?.Id ?? 0, string.Empty));
        OnQuestionStateChanged();
    }

    [RelayCommand]
    private Task FinishTestAsync() => RunAsync(async () =>
    {
        if (!CanFinishTest || sessionService.CurrentUser is null || SelectedMaterial is null) return;
        SaveCurrentAnswer();
        var answers = Tasks.Select(task => new TestAnswerDto(task.Id, testAnswers.GetValueOrDefault(task.Id, string.Empty))).ToList();
        var result = await testService.SubmitTestAsync(new SubmitTestRequest(sessionService.CurrentUser.Id, SelectedMaterial.Id, answers, testStartedAtUtc));
        StatusMessage = result.Message;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Message;
            return;
        }

        TestResult = result.Value;
        TestStarted = false;
        TestCompleted = false;
        StatusMessage = $"Итог: {TestResult!.Score}/{TestResult.MaxScore} баллов ({TestResult.Percent}%), оценка {TestGrading.FormatGrade(TestResult.Grade)}.";
        var owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ?? System.Windows.Application.Current.MainWindow;
        TestResultsWindow.ShowDialog(TestResult, owner);
        TestResult = null;
        OnMaterialStateChanged();
    });

    private void SaveCurrentAnswer()
    {
        if (CurrentTask is null) return;
        var value = CurrentTask.AnswerType == AnswerType.MultipleChoice
            ? string.Join(';', AnswerOptions.Where(x => x.IsSelected).Select(x => x.Text))
            : Answer;
        testAnswers[CurrentTask.Id] = value.Trim();
    }

    private void LoadAnswerEditor(TaskDto? task, string savedAnswer)
    {
        AnswerOptions.Clear();
        SelectedAnswerOption = null;
        Answer = savedAnswer;
        if (task is null)
        {
            OnQuestionStateChanged();
            return;
        }

        var selected = savedAnswer.Split(';', ',', '|').Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in task.Options.OrderBy(x => x.SortOrder))
        {
            var item = new AnswerOptionSelectionViewModel(option.Text, selected.Contains(option.Text));
            AnswerOptions.Add(item);
            if (task.AnswerType == AnswerType.SingleChoice && item.IsSelected) SelectedAnswerOption = item;
        }

        OnQuestionStateChanged();
    }

    private void ResetTestState()
    {
        testAnswers.Clear();
        TestStarted = false;
        TestCompleted = false;
        TestResult = null;
        CurrentTaskIndex = 0;
        Answer = string.Empty;
        AnswerOptions.Clear();
    }

    private void OnMaterialStateChanged()
    {
        OnPropertyChanged(nameof(IsSelectedMaterialTest));
        OnPropertyChanged(nameof(IsPracticeMode));
        OnPropertyChanged(nameof(CanStartTest));
        OnPropertyChanged(nameof(HasTasks));
        OnQuestionStateChanged();
    }

    private void OnQuestionStateChanged()
    {
        OnPropertyChanged(nameof(CurrentTask));
        OnPropertyChanged(nameof(QuestionProgressText));
        OnPropertyChanged(nameof(TestMaxScore));
        OnPropertyChanged(nameof(IsTextAnswer));
        OnPropertyChanged(nameof(IsSingleChoiceAnswer));
        OnPropertyChanged(nameof(IsMultipleChoiceAnswer));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanFinishTest));
    }

    public bool HasSelectedTopic => SelectedTopic is not null;
    public bool HasCatalogItems => Topics.Count > 0;
    public string CatalogSummary => Topics.Count == 0
        ? "Тесты не найдены"
        : $"{Materials.Count} {PluralizeTests(Materials.Count)} · {Topics.Count} {PluralizeTopics(Topics.Count)}";

    private static string NormalizeTopicKey(string? topic) => string.IsNullOrWhiteSpace(topic) ? "Без темы" : topic.Trim();
    private static string NormalizeSectionKey(string? section) => string.IsNullOrWhiteSpace(section) ? "Основы" : section.Trim();
    private static string DisplayTopicName(IGrouping<string, MaterialDto> group) => NormalizeTopicKey(group.First().Topic);
    private static string DisplaySectionName(IGrouping<string, MaterialDto> group) => NormalizeSectionKey(group.First().Section);
    private static string PluralizeTests(int count) => count == 1 ? "тест" : count is >= 2 and <= 4 ? "теста" : "тестов";
    private static string PluralizeTopics(int count) => count == 1 ? "тема" : count is >= 2 and <= 4 ? "темы" : "тем";
}

public sealed partial class AnswerOptionSelectionViewModel(string text, bool isSelected) : ObservableObject
{
    public string Text { get; } = text;

    [ObservableProperty]
    private bool isSelected = isSelected;
}

public sealed class TestTopicViewModel(string name, IReadOnlyList<MaterialSectionViewModel> sections)
{
    public string Name { get; } = name;
    public IReadOnlyList<MaterialSectionViewModel> Sections { get; } = sections;
    public IReadOnlyList<MaterialDto> Tests => Sections.SelectMany(x => x.Materials).ToList();
    public int TestCount => Tests.Count;
    public int SectionCount => Sections.Count;
    public string Header => Name;
    public string Meta => $"{SectionCount} разд. · {TestCount} тест.";

    public override string ToString() => Name;
}

public sealed class MaterialSectionViewModel(string name, IReadOnlyList<MaterialDto> materials)
{
    public string Name { get; } = name;
    public IReadOnlyList<MaterialDto> Materials { get; } = materials;
    public string Header => Name;
    public string Meta => $"{Materials.Count} {Materials.Count switch { 1 => "тест", >= 2 and <= 4 => "теста", _ => "тестов" }}";

    public override string ToString() => Name;
}
