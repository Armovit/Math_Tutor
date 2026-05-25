using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record AnswerOptionDto(int Id, string Text, bool IsCorrect, int SortOrder);
public sealed record TaskQuery(int? MaterialId = null, string? Search = null, DifficultyLevel? Difficulty = null, AnswerType? AnswerType = null);
public sealed record TaskDto(int Id, int EducationalMaterialId, string MaterialTitle, string Title, string Question, AnswerType AnswerType, string? CorrectAnswer, decimal MaxScore, string Explanation, DifficultyLevel Difficulty, IReadOnlyList<AnswerOptionDto> Options)
{
    public override string ToString() => Title;
}
public sealed record TaskEditDto(int Id, int EducationalMaterialId, string Title, string Question, AnswerType AnswerType, string? CorrectAnswer, decimal MaxScore, string Explanation, DifficultyLevel Difficulty, IReadOnlyList<AnswerOptionDto> Options);
public sealed record SubmitAnswerRequest(int UserId, int TaskId, string Answer);
public sealed record SubmissionDto(int Id, int UserId, int MathTaskId, string TaskTitle, string Answer, decimal Score, decimal MaxScore, SubmissionStatus Status, string Feedback, DateTime SubmittedAtUtc);
