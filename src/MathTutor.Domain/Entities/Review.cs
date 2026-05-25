namespace MathTutor.Domain.Entities;

public sealed class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int? EducationalMaterialId { get; set; }
    public EducationalMaterial? EducationalMaterial { get; set; }
    public int? MathTaskId { get; set; }
    public MathTask? MathTask { get; set; }
    public int DifficultyRating { get; set; }
    public int UsefulnessRating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
