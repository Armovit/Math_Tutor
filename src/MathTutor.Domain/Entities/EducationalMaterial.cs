using MathTutor.Domain.Enums;

namespace MathTutor.Domain.Entities;

public sealed class EducationalMaterial
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public MaterialType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public string TheoryContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<MathTask> Tasks { get; set; } = new List<MathTask>();
    public ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
