using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record ReviewDto(
    int Id,
    int UserId,
    string UserName,
    int? EducationalMaterialId,
    string MaterialTitle,
    string MaterialTopic,
    string MaterialSection,
    MaterialType? MaterialType,
    int? MathTaskId,
    string TaskTitle,
    int DifficultyRating,
    int UsefulnessRating,
    string Comment,
    DateTime CreatedAtUtc)
{
    public string TargetKind => MathTaskId is not null ? "Задание" : MaterialType switch
    {
        MathTutor.Domain.Enums.MaterialType.Test => "Тест",
        MathTutor.Domain.Enums.MaterialType.Theory => "Тема",
        MathTutor.Domain.Enums.MaterialType.Practice => "Практика",
        MathTutor.Domain.Enums.MaterialType.Lesson => "Занятие",
        _ => "Материал"
    };

    public string TargetTitle => MathTaskId is not null && !string.IsNullOrWhiteSpace(TaskTitle)
        ? TaskTitle
        : MaterialTitle;

    public string TargetContext => string.Join(" • ", new[] { MaterialTopic, MaterialSection, MaterialTitle }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed record ReviewEditDto(int UserId, int? EducationalMaterialId, int? MathTaskId, int DifficultyRating, int UsefulnessRating, string Comment);
