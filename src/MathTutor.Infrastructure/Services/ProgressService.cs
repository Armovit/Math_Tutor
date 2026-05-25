using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class ProgressService(MathTutorDbContext dbContext) : IProgressService
{
    public async Task<ProgressSummaryDto> GetStudentProgressAsync(int userId, CancellationToken cancellationToken = default)
    {
        var totalTasks = await dbContext.MathTasks.CountAsync(cancellationToken);
        var submissions = await dbContext.TaskSubmissions
            .Include(x => x.MathTask)
            .ThenInclude(x => x!.EducationalMaterial)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
        var completedTaskIds = submissions.Select(x => x.MathTaskId).Distinct().Count();
        var averageScore = submissions.Count == 0 ? 0 : submissions.Average(x => x.MaxScore == 0 ? 0 : x.Score / x.MaxScore * 100);
        var percent = totalTasks == 0 ? 0 : (decimal)completedTaskIds / totalTasks * 100;
        var recent = submissions.Take(10).Select(x => new SubmissionDto(x.Id, x.UserId, x.MathTaskId, x.MathTask?.Title ?? string.Empty, x.Answer, x.Score, x.MaxScore, x.Status, x.Feedback, x.SubmittedAtUtc)).ToList();
        var attempts = await dbContext.TestAttempts
            .Include(x => x.EducationalMaterial)
            .Include(x => x.Submissions)
            .ThenInclude(x => x.MathTask)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CompletedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);
        var recentAttempts = attempts.Select(ToTestAttemptDto).ToList();
        var allAttemptPercents = await dbContext.TestAttempts.Where(x => x.UserId == userId).Select(x => x.Percent).ToListAsync(cancellationToken);
        var bestTestPercent = allAttemptPercents.Count == 0 ? 0 : allAttemptPercents.Max();
        var averageTestPercent = allAttemptPercents.Count == 0 ? 0 : allAttemptPercents.Average();
        var topicProgress = await GetTopicProgressAsync(userId, submissions, cancellationToken);
        var recommendations = topicProgress
            .Where(x => x.ActivityCountForRecommendation() >= 2 && x.AveragePercent < 70)
            .OrderBy(x => x.AveragePercent)
            .Take(4)
            .Select(x => new StudyRecommendationDto(
                x.Topic,
                x.AveragePercent,
                x.AttemptCount + x.CompletedTasks,
                x.AveragePercent < 50
                    ? $"Срочно повторите тему «{x.Topic}»: средний результат ниже 50%."
                    : $"Закрепите тему «{x.Topic}»: результат уже неплохой, но есть запас до уверенного уровня."))
            .ToList();

        return new ProgressSummaryDto(completedTaskIds, Math.Round(averageScore, 1), Math.Round(percent, 1), recent, recentAttempts, Math.Round(bestTestPercent, 1), Math.Round(averageTestPercent, 1), topicProgress, recommendations);
    }

    public async Task<AdminStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var students = await dbContext.Users.Where(x => x.Role == UserRole.Student).ToListAsync(cancellationToken);
        var totalTasks = await dbContext.MathTasks.CountAsync(cancellationToken);
        var submissions = await dbContext.TaskSubmissions.Include(x => x.User).ToListAsync(cancellationToken);
        var studentStats = students.Select(student =>
        {
            var studentSubmissions = submissions.Where(x => x.UserId == student.Id).ToList();
            var completed = studentSubmissions.Select(x => x.MathTaskId).Distinct().Count();
            var average = studentSubmissions.Count == 0 ? 0 : studentSubmissions.Average(x => x.MaxScore == 0 ? 0 : x.Score / x.MaxScore * 100);
            var completion = totalTasks == 0 ? 0 : (decimal)completed / totalTasks * 100;
            return new StudentStatisticDto(student.Id, student.FullName, completed, Math.Round(average, 1), Math.Round(completion, 1));
        }).OrderByDescending(x => x.AverageScore).ToList();

        var attemptPercents = await dbContext.TestAttempts.Select(x => x.Percent).ToListAsync(cancellationToken);
        var globalAverage = attemptPercents.Count > 0
            ? attemptPercents.Average()
            : submissions.Count == 0 ? 0 : submissions.Average(x => x.MaxScore == 0 ? 0 : x.Score / x.MaxScore * 100);
        return new AdminStatisticsDto(
            students.Count,
            await dbContext.EducationalMaterials.CountAsync(cancellationToken),
            await dbContext.EducationalMaterials.CountAsync(x => x.IsPublished, cancellationToken),
            submissions.Count,
            Math.Round(globalAverage, 1),
            studentStats);
    }

    private static TestAttemptDto ToTestAttemptDto(MathTutor.Domain.Entities.TestAttempt attempt)
    {
        var submissions = attempt.Submissions
            .OrderBy(x => x.Id)
            .Select(x => new SubmissionDto(x.Id, x.UserId, x.MathTaskId, x.MathTask?.Title ?? string.Empty, x.Answer, x.Score, x.MaxScore, x.Status, x.Feedback, x.SubmittedAtUtc))
            .ToList();
        return new TestAttemptDto(attempt.Id, attempt.UserId, attempt.EducationalMaterialId, attempt.EducationalMaterial?.Title ?? string.Empty, attempt.EducationalMaterial?.Topic ?? string.Empty, attempt.EducationalMaterial?.Section ?? string.Empty, attempt.Score, attempt.MaxScore, attempt.Percent, attempt.Grade, attempt.Status, attempt.StartedAtUtc, attempt.CompletedAtUtc, submissions);
    }

    private async Task<IReadOnlyList<TopicProgressDto>> GetTopicProgressAsync(int userId, IReadOnlyList<MathTutor.Domain.Entities.TaskSubmission> submissions, CancellationToken cancellationToken)
    {
        var totalTasksByTopic = await dbContext.MathTasks
            .Include(x => x.EducationalMaterial)
            .AsNoTracking()
            .Where(x => x.EducationalMaterial != null && x.EducationalMaterial.IsPublished)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.EducationalMaterial!.Topic) ? "Без темы" : x.EducationalMaterial.Topic)
            .Select(x => new { Topic = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var attempts = await dbContext.TestAttempts
            .Include(x => x.EducationalMaterial)
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                Topic = string.IsNullOrWhiteSpace(x.EducationalMaterial!.Topic) ? "Без темы" : x.EducationalMaterial.Topic,
                x.Percent
            })
            .ToListAsync(cancellationToken);

        var submissionRows = submissions
            .Where(x => x.MathTask?.EducationalMaterial is not null)
            .Select(x => new
            {
                Topic = NormalizeTopic(x.MathTask!.EducationalMaterial!.Topic),
                Percent = x.MaxScore == 0 ? 0 : x.Score / x.MaxScore * 100,
                x.MathTaskId
            })
            .ToList();

        var topics = totalTasksByTopic.Select(x => x.Topic)
            .Concat(attempts.Select(x => NormalizeTopic(x.Topic)))
            .Concat(submissionRows.Select(x => x.Topic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return topics.Select(topic =>
        {
            var topicAttempts = attempts.Where(x => string.Equals(NormalizeTopic(x.Topic), topic, StringComparison.OrdinalIgnoreCase)).ToList();
            var topicSubmissions = submissionRows.Where(x => string.Equals(x.Topic, topic, StringComparison.OrdinalIgnoreCase)).ToList();
            var values = topicAttempts.Select(x => x.Percent).Concat(topicSubmissions.Select(x => x.Percent)).ToList();
            var totalTasks = totalTasksByTopic.FirstOrDefault(x => string.Equals(x.Topic, topic, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
            var completedTasks = topicSubmissions.Select(x => x.MathTaskId).Distinct().Count();

            return new TopicProgressDto(
                topic,
                topicAttempts.Count,
                completedTasks,
                totalTasks,
                values.Count == 0 ? 0 : Math.Round(values.Average(), 1),
                values.Count == 0 ? 0 : Math.Round(values.Max(), 1),
                totalTasks == 0 ? 0 : Math.Round((decimal)completedTasks / totalTasks * 100, 1));
        }).ToList();
    }

    private static string NormalizeTopic(string? topic) => string.IsNullOrWhiteSpace(topic) ? "Без темы" : topic.Trim();
}

file static class TopicProgressExtensions
{
    public static int ActivityCountForRecommendation(this TopicProgressDto topic) => topic.AttemptCount + topic.CompletedTasks;
}
