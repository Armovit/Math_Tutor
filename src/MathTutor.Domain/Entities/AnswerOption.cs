namespace MathTutor.Domain.Entities;

public sealed class AnswerOption
{
    public int Id { get; set; }
    public int MathTaskId { get; set; }
    public MathTask? MathTask { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}
