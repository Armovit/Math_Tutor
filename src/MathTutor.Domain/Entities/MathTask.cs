using MathTutor.Domain.Enums;

namespace MathTutor.Domain.Entities;

public sealed class MathTask
{
    public int Id { get; set; }
    public int EducationalMaterialId { get; set; }
    public EducationalMaterial? EducationalMaterial { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public AnswerType AnswerType { get; set; } = AnswerType.Text;
    public string? CorrectAnswer { get; set; }
    public decimal MaxScore { get; set; } = 1;
    public string Explanation { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    public ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
