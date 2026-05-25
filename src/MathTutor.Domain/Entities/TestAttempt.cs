using MathTutor.Domain.Enums;

namespace MathTutor.Domain.Entities;

public sealed class TestAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int EducationalMaterialId { get; set; }
    public EducationalMaterial? EducationalMaterial { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percent { get; set; }
    public int Grade { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.AutoChecked;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
}
