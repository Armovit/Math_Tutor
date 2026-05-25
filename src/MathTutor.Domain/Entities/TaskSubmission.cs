using MathTutor.Domain.Enums;

namespace MathTutor.Domain.Entities;

public sealed class TaskSubmission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int MathTaskId { get; set; }
    public MathTask? MathTask { get; set; }
    public int? TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }
    public string Answer { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public string Feedback { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
