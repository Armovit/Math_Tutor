using MathTutor.Domain.Enums;

namespace MathTutor.Domain.Entities;

public sealed class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}
