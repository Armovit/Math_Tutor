using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record NotificationDto(int Id, int UserId, string Title, string Message, NotificationType Type, bool IsRead, DateTime CreatedAtUtc);
public sealed record NotificationCreateDto(int UserId, string Title, string Message, NotificationType Type);
