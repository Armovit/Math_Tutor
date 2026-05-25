using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class TestService(MathTutorDbContext dbContext, IEmailService emailService) : ITestService
{
    public async Task<OperationResult<TestAttemptDto>> SubmitTestAsync(SubmitTestRequest request, CancellationToken cancellationToken = default)
    {
        var material = await dbContext.EducationalMaterials
            .Include(x => x.Tasks)
            .ThenInclude(x => x.AnswerOptions)
            .FirstOrDefaultAsync(x => x.Id == request.MaterialId, cancellationToken);

        if (material is null) return OperationResult<TestAttemptDto>.Failure("Тест не найден.");
        if (material.Type != MaterialType.Test) return OperationResult<TestAttemptDto>.Failure("Выбранный материал не является тестом.");
        if (!material.Tasks.Any()) return OperationResult<TestAttemptDto>.Failure("В тесте пока нет вопросов.");

        var answersByTask = request.Answers.ToDictionary(x => x.TaskId, x => x.Answer ?? string.Empty);
        var startedAt = request.StartedAtUtc ?? DateTime.UtcNow;
        var completedAt = DateTime.UtcNow;
        var attempt = new TestAttempt
        {
            UserId = request.UserId,
            EducationalMaterialId = material.Id,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Status = SubmissionStatus.AutoChecked
        };

        foreach (var task in material.Tasks.OrderBy(x => x.Id))
        {
            var answer = answersByTask.GetValueOrDefault(task.Id, string.Empty);
            var check = AnswerChecker.Check(task, answer);
            attempt.Submissions.Add(new TaskSubmission
            {
                UserId = request.UserId,
                MathTaskId = task.Id,
                Answer = answer.Trim(),
                Score = check.Score,
                MaxScore = task.MaxScore,
                Status = check.Status,
                Feedback = check.Feedback,
                SubmittedAtUtc = completedAt
            });
        }

        attempt.Score = attempt.Submissions.Sum(x => x.Score);
        attempt.MaxScore = attempt.Submissions.Sum(x => x.MaxScore);
        attempt.Percent = attempt.MaxScore == 0 ? 0 : Math.Round(attempt.Score / attempt.MaxScore * 100, 2);
        attempt.Grade = TestGrading.CalculateGrade(attempt.Percent);
        attempt.Status = attempt.Submissions.Any(x => x.Status == SubmissionStatus.NeedsReview)
            ? SubmissionStatus.NeedsReview
            : SubmissionStatus.AutoChecked;

        dbContext.TestAttempts.Add(attempt);
        dbContext.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Title = "Тест завершён",
            Message = $"Тест '{material.Title}': {attempt.Score}/{attempt.MaxScore} баллов ({attempt.Percent}%), оценка {TestGrading.FormatGrade(attempt.Grade)}.",
            Type = NotificationType.Grade,
            CreatedAtUtc = completedAt,
            RelatedEntityType = nameof(TestAttempt)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await SendGradeEmailAsync(request.UserId, "Тест завершён", $"Тест '{material.Title}': {attempt.Score}/{attempt.MaxScore} баллов ({attempt.Percent}%), оценка {TestGrading.FormatGrade(attempt.Grade)}.", cancellationToken);
        return OperationResult<TestAttemptDto>.Success(ToDto(attempt, material), "Тест завершён.");
    }

    public async Task<IReadOnlyList<TestAttemptDto>> GetAttemptsAsync(int userId, int? materialId = null, CancellationToken cancellationToken = default)
        => await GetAttemptsAsync(new TestAttemptQuery(userId, materialId), cancellationToken);

    public async Task<IReadOnlyList<TestAttemptDto>> GetAttemptsAsync(TestAttemptQuery request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TestAttempts
            .Include(x => x.EducationalMaterial)
            .Include(x => x.Submissions)
            .ThenInclude(x => x.MathTask)
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId);

        if (request.MaterialId is not null) query = query.Where(x => x.EducationalMaterialId == request.MaterialId);
        if (!string.IsNullOrWhiteSpace(request.Topic)) query = query.Where(x => x.EducationalMaterial != null && x.EducationalMaterial.Topic == request.Topic.Trim());
        if (request.FromUtc is not null) query = query.Where(x => x.CompletedAtUtc >= request.FromUtc);
        if (request.ToUtc is not null) query = query.Where(x => x.CompletedAtUtc <= request.ToUtc);

        query = query.OrderByDescending(x => x.CompletedAtUtc);
        if (request.Skip is > 0) query = query.Skip(request.Skip.Value);
        if (request.Take is > 0) query = query.Take(request.Take.Value);

        var attempts = await query.ToListAsync(cancellationToken);
        return attempts.Select(x => ToDto(x, x.EducationalMaterial!)).ToList();
    }

    private static TestAttemptDto ToDto(TestAttempt attempt, EducationalMaterial material)
    {
        var submissions = attempt.Submissions
            .OrderBy(x => x.Id)
            .Select(x => new SubmissionDto(x.Id, x.UserId, x.MathTaskId, x.MathTask?.Title ?? string.Empty, x.Answer, x.Score, x.MaxScore, x.Status, x.Feedback, x.SubmittedAtUtc))
            .ToList();

        return new TestAttemptDto(attempt.Id, attempt.UserId, attempt.EducationalMaterialId, material.Title, material.Topic, material.Section, attempt.Score, attempt.MaxScore, attempt.Percent, attempt.Grade, attempt.Status, attempt.StartedAtUtc, attempt.CompletedAtUtc, submissions);
    }

    private async Task SendGradeEmailAsync(int userId, string subject, string body, CancellationToken cancellationToken)
    {
        var email = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(email))
        {
            await emailService.SendAsync(email, subject, body, cancellationToken);
        }
    }
}
