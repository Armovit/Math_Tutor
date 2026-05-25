using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class TaskManagementViewModel(ITaskService taskService, IMaterialService materialService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private TaskDto? selectedTask;

    [ObservableProperty]
    private MaterialDto? selectedMaterial;

    [ObservableProperty]
    private MaterialDto? selectedFilterMaterial;

    [ObservableProperty]
    private string search = string.Empty;

    [ObservableProperty]
    private DifficultyLevel? difficultyFilter;

    [ObservableProperty]
    private AnswerType? answerTypeFilter;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editQuestion = string.Empty;

    [ObservableProperty]
    private string editCorrectAnswer = string.Empty;

    [ObservableProperty]
    private string editExplanation = string.Empty;

    [ObservableProperty]
    private decimal editMaxScore = 1;

    [ObservableProperty]
    private AnswerType editAnswerType = AnswerType.Number;

    [ObservableProperty]
    private DifficultyLevel editDifficulty = DifficultyLevel.Beginner;

    [ObservableProperty]
    private string newOptionText = string.Empty;

    public ObservableCollection<TaskDto> Tasks { get; } = new();
    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public ObservableCollection<AnswerOptionEditViewModel> EditOptions { get; } = new();
    public IEnumerable<AnswerType> AnswerTypes { get; } = Enum.GetValues<AnswerType>();
    public IEnumerable<AnswerType?> AnswerTypeFilters { get; } = new AnswerType?[] { null }.Concat(Enum.GetValues<AnswerType>().Cast<AnswerType?>());
    public IEnumerable<DifficultyLevel> DifficultyLevels { get; } = Enum.GetValues<DifficultyLevel>();
    public IEnumerable<DifficultyLevel?> DifficultyFilters { get; } = new DifficultyLevel?[] { null }.Concat(Enum.GetValues<DifficultyLevel>().Cast<DifficultyLevel?>());
    public override string Title => "Задачи";
    public bool UsesAnswerOptions => EditAnswerType is AnswerType.SingleChoice or AnswerType.MultipleChoice;

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        if (value is null) return;
        SelectedMaterial = Materials.FirstOrDefault(x => x.Id == value.EducationalMaterialId);
        EditTitle = value.Title;
        EditQuestion = value.Question;
        EditCorrectAnswer = value.CorrectAnswer ?? string.Empty;
        EditExplanation = value.Explanation;
        EditMaxScore = value.MaxScore;
        EditAnswerType = value.AnswerType;
        EditDifficulty = value.Difficulty;
        EditOptions.Clear();
        foreach (var option in value.Options.OrderBy(x => x.SortOrder))
        {
            EditOptions.Add(new AnswerOptionEditViewModel(option.Text, option.IsCorrect));
        }
        SyncCorrectAnswerFromOptions();
    }

    partial void OnEditAnswerTypeChanged(AnswerType value)
    {
        OnPropertyChanged(nameof(UsesAnswerOptions));
        SyncCorrectAnswerFromOptions();
    }

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        await LoadDataAsync();
    });

    [RelayCommand]
    private Task ApplyFiltersAsync() => LoadAsync();

    [RelayCommand]
    private Task ResetFiltersAsync()
    {
        SelectedFilterMaterial = null;
        Search = string.Empty;
        DifficultyFilter = null;
        AnswerTypeFilter = null;
        return LoadAsync();
    }

    [RelayCommand]
    private void NewTask()
    {
        SelectedTask = null;
        EditTitle = string.Empty;
        EditQuestion = string.Empty;
        EditCorrectAnswer = string.Empty;
        EditExplanation = string.Empty;
        EditMaxScore = 1;
        EditAnswerType = AnswerType.Number;
        EditDifficulty = DifficultyLevel.Beginner;
        EditOptions.Clear();
        NewOptionText = string.Empty;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        ErrorMessage = string.Empty;
        if (SelectedMaterial is null) { ErrorMessage = "Выберите материал."; return; }
        if (UsesAnswerOptions && EditOptions.Count < 2) { ErrorMessage = "Добавьте минимум два варианта ответа."; return; }
        if (UsesAnswerOptions && EditOptions.All(x => !x.IsCorrect)) { ErrorMessage = "Отметьте правильный вариант ответа."; return; }
        SyncCorrectAnswerFromOptions();
        var options = EditOptions
            .Select((x, index) => new AnswerOptionDto(0, x.Text, x.IsCorrect, index + 1))
            .ToList();
        var result = await taskService.SaveTaskAsync(new TaskEditDto(SelectedTask?.Id ?? 0, SelectedMaterial.Id, EditTitle, EditQuestion, EditAnswerType, EditCorrectAnswer, EditMaxScore, EditExplanation, EditDifficulty, options));
        StatusMessage = result.Message;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Message;
            return;
        }

        await LoadDataAsync(result.Value?.Id, ensureSelectedVisible: true);
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (SelectedTask is null) return;
        var result = await taskService.DeleteTaskAsync(SelectedTask.Id);
        StatusMessage = result.Message;
        await LoadAsync();
        NewTask();
    });

    [RelayCommand]
    private void AddOption()
    {
        if (string.IsNullOrWhiteSpace(NewOptionText)) return;
        EditOptions.Add(new AnswerOptionEditViewModel(NewOptionText.Trim(), EditOptions.Count == 0));
        NewOptionText = string.Empty;
        SyncCorrectAnswerFromOptions();
    }

    [RelayCommand]
    private void RemoveOption(AnswerOptionEditViewModel? option)
    {
        if (option is null) return;
        EditOptions.Remove(option);
        SyncCorrectAnswerFromOptions();
    }

    [RelayCommand]
    private void SyncCorrectAnswerFromOptions()
    {
        if (!UsesAnswerOptions) return;
        if (EditAnswerType == AnswerType.SingleChoice)
        {
            var firstCorrect = EditOptions.FirstOrDefault(x => x.IsCorrect);
            foreach (var option in EditOptions)
            {
                option.IsCorrect = ReferenceEquals(option, firstCorrect);
            }
        }

        EditCorrectAnswer = string.Join(';', EditOptions.Where(x => x.IsCorrect).Select(x => x.Text));
    }

    private async Task LoadDataAsync(int? selectTaskId = null, bool ensureSelectedVisible = false)
    {
        var selectedMaterialId = SelectedMaterial?.Id;
        var selectedFilterMaterialId = SelectedFilterMaterial?.Id;

        Materials.Clear();
        foreach (var item in await materialService.GetMaterialsAsync(new MaterialQuery(SortBy: "Type"))) Materials.Add(item);

        SelectedMaterial = selectedMaterialId is null
            ? SelectedMaterial ?? Materials.FirstOrDefault(x => x.Type == MaterialType.Test) ?? Materials.FirstOrDefault()
            : Materials.FirstOrDefault(x => x.Id == selectedMaterialId.Value);
        SelectedFilterMaterial = selectedFilterMaterialId is null ? null : Materials.FirstOrDefault(x => x.Id == selectedFilterMaterialId.Value);

        Tasks.Clear();
        foreach (var item in await taskService.GetTasksAsync(new TaskQuery(SelectedFilterMaterial?.Id, Search, DifficultyFilter, AnswerTypeFilter))) Tasks.Add(item);

        if (selectTaskId is not null)
        {
            SelectedTask = Tasks.FirstOrDefault(x => x.Id == selectTaskId.Value);
            if (SelectedTask is null && ensureSelectedVisible)
            {
                SelectedFilterMaterial = null;
                Search = string.Empty;
                DifficultyFilter = null;
                AnswerTypeFilter = null;
                await LoadDataAsync(selectTaskId);
            }
        }
    }
}

public sealed partial class AnswerOptionEditViewModel(string text, bool isCorrect) : ObservableObject
{
    [ObservableProperty]
    private string text = text;

    [ObservableProperty]
    private bool isCorrect = isCorrect;
}
