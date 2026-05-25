using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<OperationResult> AddAsync(NotificationCreateDto notification, CancellationToken cancellationToken = default);
    Task<OperationResult> MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);
}
