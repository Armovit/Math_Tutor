using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record MaterialDto(int Id, string Title, string Topic, string Section, MaterialType Type, string Description, DifficultyLevel Difficulty, string TheoryContent, bool IsPublished, int TaskCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc)
{
    public override string ToString() => Title;
}
public sealed record MaterialEditDto(int Id, string Title, string Topic, string Section, MaterialType Type, string Description, DifficultyLevel Difficulty, string TheoryContent, bool IsPublished);
public sealed record MaterialQuery(string? Search = null, string? Topic = null, string? Section = null, MaterialType? Type = null, DifficultyLevel? Difficulty = null, bool PublishedOnly = false, string SortBy = "Title");
