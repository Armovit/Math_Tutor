namespace MathTutor.Application.DTOs;

public sealed record ProgressSummaryDto(
    int CompletedTasks,
    decimal AverageScore,
    decimal CompletionPercent,
    IReadOnlyList<SubmissionDto> RecentSubmissions,
    IReadOnlyList<TestAttemptDto> RecentTestAttempts,
    decimal BestTestPercent,
    decimal AverageTestPercent,
    IReadOnlyList<TopicProgressDto> TopicProgress,
    IReadOnlyList<StudyRecommendationDto> Recommendations);

public sealed record TopicProgressDto(
    string Topic,
    int AttemptCount,
    int CompletedTasks,
    int TotalTasks,
    decimal AveragePercent,
    decimal BestPercent,
    decimal CompletionPercent);

public sealed record StudyRecommendationDto(
    string Topic,
    decimal AveragePercent,
    int ActivityCount,
    string Message);

public sealed record AdminStatisticsDto(int StudentsCount, int MaterialsCount, int PublishedMaterialsCount, int SubmissionsCount, decimal AverageScore, IReadOnlyList<StudentStatisticDto> Students);
public sealed record StudentStatisticDto(int UserId, string FullName, int CompletedTasks, decimal AverageScore, decimal CompletionPercent);
