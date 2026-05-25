using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class TheoryViewModel(IMaterialService materialService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private string search = string.Empty;

    [ObservableProperty]
    private string topicFilter = string.Empty;

    [ObservableProperty]
    private string sectionFilter = string.Empty;

    [ObservableProperty]
    private DifficultyLevel? difficultyFilter;

    [ObservableProperty]
    private TheoryTopicViewModel? selectedTopic;

    [ObservableProperty]
    private MaterialDto? selectedTheory;

    public ObservableCollection<TheoryTopicViewModel> Topics { get; } = new();
    public IEnumerable<DifficultyLevel?> DifficultyFilters { get; } = new DifficultyLevel?[] { null }.Concat(Enum.GetValues<DifficultyLevel>().Cast<DifficultyLevel?>());
    public override string Title => "Теория";

    partial void OnSelectedTopicChanged(TheoryTopicViewModel? value)
    {
        SelectedTheory = value?.Sections.SelectMany(x => x.Materials).FirstOrDefault();
    }

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        Topics.Clear();
        var materials = await materialService.GetMaterialsAsync(new MaterialQuery(Search: Search, Topic: TopicFilter, Section: SectionFilter, Type: MaterialType.Theory, Difficulty: DifficultyFilter, PublishedOnly: true, SortBy: "Topic"));
        foreach (var topic in materials.GroupBy(x => string.IsNullOrWhiteSpace(x.Topic) ? "Без темы" : x.Topic).OrderBy(x => x.Key))
        {
            var sections = topic
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Section) ? "Основы" : x.Section)
                .OrderBy(x => x.Key)
                .Select(x => new MaterialSectionViewModel(x.Key, x.OrderBy(m => m.Title).ToList()))
                .ToList();
            Topics.Add(new TheoryTopicViewModel(topic.Key, sections));
        }

        SelectedTopic = Topics.FirstOrDefault();
    });
}

public sealed class TheoryTopicViewModel(string name, IReadOnlyList<MaterialSectionViewModel> sections)
{
    public string Name { get; } = name;
    public IReadOnlyList<MaterialSectionViewModel> Sections { get; } = sections;
    public IReadOnlyList<MaterialDto> Materials => Sections.SelectMany(x => x.Materials).ToList();
    public string Header => $"{Name} ({Materials.Count})";
}
