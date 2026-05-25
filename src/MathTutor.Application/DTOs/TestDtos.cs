using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record TestAnswerDto(int TaskId, string Answer);

public sealed record SubmitTestRequest(int UserId, int MaterialId, IReadOnlyList<TestAnswerDto> Answers, DateTime? StartedAtUtc = null);

public sealed record TestAttemptQuery(
    int UserId,
    int? MaterialId = null,
    string? Topic = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? Skip = null,
    int? Take = null);

public sealed record TestAttemptDto(
    int Id,
    int UserId,
    int MaterialId,
    string MaterialTitle,
    string MaterialTopic,
    string MaterialSection,
    decimal Score,
    decimal MaxScore,
    decimal Percent,
    int Grade,
    SubmissionStatus Status,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    IReadOnlyList<SubmissionDto> Submissions);
