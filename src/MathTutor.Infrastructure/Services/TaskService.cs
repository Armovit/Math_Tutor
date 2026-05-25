using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class TaskService(MathTutorDbContext dbContext, IEmailService emailService) : ITaskService
{
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(int? materialId = null, CancellationToken cancellationToken = default)
        => await GetTasksAsync(new TaskQuery(MaterialId: materialId), cancellationToken);

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(TaskQuery request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MathTasks.Include(x => x.EducationalMaterial).Include(x => x.AnswerOptions).AsNoTracking().AsQueryable();
        if (request.MaterialId is not null) query = query.Where(x => x.EducationalMaterialId == request.MaterialId);
        if (request.Difficulty is not null) query = query.Where(x => x.Difficulty == request.Difficulty);
        if (request.AnswerType is not null) query = query.Where(x => x.AnswerType == request.AnswerType);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                x.Question.ToLower().Contains(search) ||
                (x.CorrectAnswer != null && x.CorrectAnswer.ToLower().Contains(search)) ||
                x.Explanation.ToLower().Contains(search) ||
                x.EducationalMaterial!.Title.ToLower().Contains(search) ||
                x.EducationalMaterial.Topic.ToLower().Contains(search) ||
                x.EducationalMaterial.Section.ToLower().Contains(search));
        }

        return await query.OrderBy(x => x.EducationalMaterial!.Title).ThenBy(x => x.Title).Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<TaskDto>> SaveTaskAsync(TaskEditDto task, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task.Title)) return OperationResult<TaskDto>.Failure("Введите название задания.");
        if (string.IsNullOrWhiteSpace(task.Question)) return OperationResult<TaskDto>.Failure("Введите условие задания.");
        if (task.MaxScore <= 0) return OperationResult<TaskDto>.Failure("Максимальный балл должен быть больше 0.");

        MathTask entity;
        if (task.Id == 0)
        {
            entity = new MathTask { CreatedAtUtc = DateTime.UtcNow };
            dbContext.MathTasks.Add(entity);
        }
        else
        {
            entity = await dbContext.MathTasks.Include(x => x.AnswerOptions).Include(x => x.EducationalMaterial).FirstOrDefaultAsync(x => x.Id == task.Id, cancellationToken)
                ?? throw new InvalidOperationException("Задание не найдено.");
            dbContext.AnswerOptions.RemoveRange(entity.AnswerOptions);
        }

        entity.EducationalMaterialId = task.EducationalMaterialId;
        entity.Title = task.Title.Trim();
        entity.Question = task.Question.Trim();
        entity.AnswerType = task.AnswerType;
        entity.CorrectAnswer = task.CorrectAnswer?.Trim();
        entity.MaxScore = task.MaxScore;
        entity.Explanation = task.Explanation.Trim();
        entity.Difficulty = task.Difficulty;
        entity.AnswerOptions = task.Options.Select(x => new AnswerOption { Text = x.Text.Trim(), IsCorrect = x.IsCorrect, SortOrder = x.SortOrder }).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        entity = await dbContext.MathTasks.Include(x => x.EducationalMaterial).Include(x => x.AnswerOptions).FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return OperationResult<TaskDto>.Success(ToDto(entity), "Задание сохранено.");
    }

    public async Task<OperationResult> DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MathTasks.FindAsync([id], cancellationToken);
        if (entity is null) return OperationResult.Failure("Задание не найдено.");
        dbContext.MathTasks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Задание удалено.");
    }

    public async Task<OperationResult<SubmissionDto>> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.MathTasks.Include(x => x.EducationalMaterial).Include(x => x.AnswerOptions).FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
        if (task is null) return OperationResult<SubmissionDto>.Failure("Задание не найдено.");

        var check = AnswerChecker.Check(task, request.Answer);
        var submission = new TaskSubmission
        {
            UserId = request.UserId,
            MathTaskId = task.Id,
            Answer = request.Answer.Trim(),
            Score = check.Score,
            MaxScore = task.MaxScore,
            Status = check.Status,
            Feedback = check.Feedback,
            SubmittedAtUtc = DateTime.UtcNow
        };

        dbContext.TaskSubmissions.Add(submission);
        dbContext.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Title = "Решение проверено",
            Message = $"Задание '{task.Title}': {check.Feedback}",
            Type = NotificationType.Grade,
            CreatedAtUtc = DateTime.UtcNow,
            RelatedEntityType = nameof(MathTask),
            RelatedEntityId = task.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendGradeEmailAsync(request.UserId, "Решение проверено", $"Задание '{task.Title}': {check.Feedback}", cancellationToken);
        return OperationResult<SubmissionDto>.Success(ToDto(submission, task), "Решение отправлено.");
    }

    private static TaskDto ToDto(MathTask x) => new(x.Id, x.EducationalMaterialId, x.EducationalMaterial?.Title ?? string.Empty, x.Title, x.Question, x.AnswerType, x.CorrectAnswer, x.MaxScore, x.Explanation, x.Difficulty, x.AnswerOptions.OrderBy(o => o.SortOrder).Select(o => new AnswerOptionDto(o.Id, o.Text, o.IsCorrect, o.SortOrder)).ToList());
    private static SubmissionDto ToDto(TaskSubmission x, MathTask task) => new(x.Id, x.UserId, x.MathTaskId, task.Title, x.Answer, x.Score, x.MaxScore, x.Status, x.Feedback, x.SubmittedAtUtc);

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
