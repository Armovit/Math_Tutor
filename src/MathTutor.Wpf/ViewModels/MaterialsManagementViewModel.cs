using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class MaterialsManagementViewModel(IMaterialService materialService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private MaterialDto? selectedMaterial;

    [ObservableProperty]
    private string search = string.Empty;

    [ObservableProperty]
    private string topicFilter = string.Empty;

    [ObservableProperty]
    private string sectionFilter = string.Empty;

    [ObservableProperty]
    private MaterialType? typeFilter;

    [ObservableProperty]
    private DifficultyLevel? difficultyFilter;

    [ObservableProperty]
    private bool publishedOnlyFilter;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editTopic = string.Empty;

    [ObservableProperty]
    private string editSection = string.Empty;

    [ObservableProperty]
    private string editDescription = string.Empty;

    [ObservableProperty]
    private string editTheory = string.Empty;

    [ObservableProperty]
    private MaterialType editType = MaterialType.Theory;

    [ObservableProperty]
    private DifficultyLevel editDifficulty = DifficultyLevel.Beginner;

    [ObservableProperty]
    private bool editIsPublished = true;

    [ObservableProperty]
    private bool isCreatingNewMaterial;

    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public IEnumerable<MaterialType> MaterialTypes { get; } = Enum.GetValues<MaterialType>();
    public IEnumerable<MaterialType?> MaterialTypeFilters { get; } = new MaterialType?[] { null }.Concat(Enum.GetValues<MaterialType>().Cast<MaterialType?>());
    public IEnumerable<DifficultyLevel> DifficultyLevels { get; } = Enum.GetValues<DifficultyLevel>();
    public IEnumerable<DifficultyLevel?> DifficultyFilters { get; } = new DifficultyLevel?[] { null }.Concat(Enum.GetValues<DifficultyLevel>().Cast<DifficultyLevel?>());
    public override string Title => "Материалы";
    public bool IsEditingTest => EditType == MaterialType.Test;

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        if (value is null) return;
        IsCreatingNewMaterial = false;
        EditTitle = value.Title;
        EditTopic = value.Topic;
        EditSection = value.Section;
        EditDescription = value.Description;
        EditTheory = value.TheoryContent;
        EditType = value.Type;
        EditDifficulty = value.Difficulty;
        EditIsPublished = value.IsPublished;
    }

    partial void OnEditTypeChanged(MaterialType value)
    {
        OnPropertyChanged(nameof(IsEditingTest));
    }

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        await LoadMaterialsAsync();
    });

    [RelayCommand]
    private void NewMaterial()
    {
        IsCreatingNewMaterial = true;
        SelectedMaterial = null;
        ErrorMessage = string.Empty;
        StatusMessage = "Заполните форму и нажмите «Сохранить материал».";
        EditTitle = string.Empty;
        EditTopic = string.Empty;
        EditSection = string.Empty;
        EditDescription = string.Empty;
        EditTheory = string.Empty;
        EditType = MaterialType.Theory;
        EditDifficulty = DifficultyLevel.Beginner;
        EditIsPublished = true;
    }

    [RelayCommand]
    private void NewTestMaterial()
    {
        NewMaterial();
        EditType = MaterialType.Test;
        EditTitle = $"Новый тест {DateTime.Now:HHmm}";
        EditTopic = "новая тема";
        EditSection = "тесты";
        EditDescription = "Кратко опишите, какие знания проверяет тест.";
        EditTheory = "Инструкция: внимательно прочитайте вопросы и выберите или введите правильные ответы.";
        StatusMessage = "Создайте карточку теста, сохраните её, затем добавьте вопросы в разделе «Задачи».";
    }

    [RelayCommand]
    private Task ResetFiltersAsync()
    {
        Search = string.Empty;
        TopicFilter = string.Empty;
        SectionFilter = string.Empty;
        TypeFilter = null;
        DifficultyFilter = null;
        PublishedOnlyFilter = false;
        return LoadAsync();
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        ErrorMessage = string.Empty;
        if (!IsCreatingNewMaterial && SelectedMaterial is null)
        {
            ErrorMessage = "Выберите материал в таблице или нажмите «Новый материал» / «Новый тест».";
            return;
        }

        var adminId = sessionService.CurrentUser?.Id ?? 0;
        var materialId = IsCreatingNewMaterial ? 0 : SelectedMaterial?.Id ?? 0;
        var result = await materialService.SaveMaterialAsync(new MaterialEditDto(materialId, EditTitle, EditTopic, EditSection, EditType, EditDescription, EditDifficulty, EditTheory, EditIsPublished), adminId);
        StatusMessage = result.Message;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Message;
            return;
        }

        await LoadMaterialsAsync(result.Value?.Id, ensureSelectedVisible: true);
        IsCreatingNewMaterial = false;
    });

    [RelayCommand]
    private Task PublishAsync() => SetPublicationAsync(true);

    [RelayCommand]
    private Task UnpublishAsync() => SetPublicationAsync(false);

    private async Task SetPublicationAsync(bool isPublished)
    {
        EditIsPublished = isPublished;
        await SaveAsync();
    }

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (SelectedMaterial is null) return;
        var result = await materialService.DeleteMaterialAsync(SelectedMaterial.Id);
        StatusMessage = result.Message;
        await LoadAsync();
        NewMaterial();
    });

    private async Task LoadMaterialsAsync(int? selectMaterialId = null, bool ensureSelectedVisible = false)
    {
        var items = await materialService.GetMaterialsAsync(new MaterialQuery(Search: Search, Topic: TopicFilter, Section: SectionFilter, Type: TypeFilter, Difficulty: DifficultyFilter, PublishedOnly: PublishedOnlyFilter, SortBy: "Topic"));
        Materials.Clear();
        foreach (var item in items) Materials.Add(item);

        if (selectMaterialId is not null)
        {
            SelectedMaterial = Materials.FirstOrDefault(x => x.Id == selectMaterialId.Value);
            if (SelectedMaterial is null && ensureSelectedVisible)
            {
                Search = string.Empty;
                TopicFilter = string.Empty;
                SectionFilter = string.Empty;
                TypeFilter = null;
                DifficultyFilter = null;
                PublishedOnlyFilter = false;
                await LoadMaterialsAsync(selectMaterialId);
            }
        }
    }
}
