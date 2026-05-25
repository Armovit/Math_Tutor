using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class NotificationService(MathTutorDbContext dbContext) : INotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult> AddAsync(NotificationCreateDto notification, CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(new Notification { UserId = notification.UserId, Title = notification.Title.Trim(), Message = notification.Message.Trim(), Type = notification.Type, CreatedAtUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Уведомление создано.");
    }

    public async Task<OperationResult> MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notifications.FindAsync([notificationId], cancellationToken);
        if (entity is null) return OperationResult.Failure("Уведомление не найдено.");
        entity.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Уведомление прочитано.");
    }

    private static NotificationDto ToDto(Notification x) => new(x.Id, x.UserId, x.Title, x.Message, x.Type, x.IsRead, x.CreatedAtUtc);
}
