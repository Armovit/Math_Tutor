using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.ViewModels;

public sealed class TestResultsWindowViewModel
{
    public TestResultsWindowViewModel(TestAttemptDto attempt)
    {
        Attempt = attempt;
        Questions = attempt.Submissions
            .Select((submission, index) => new TestQuestionResultViewModel(index + 1, submission))
            .ToList();
    }

    public TestAttemptDto Attempt { get; }
    public IReadOnlyList<TestQuestionResultViewModel> Questions { get; }
    public string MaterialTitle => Attempt.MaterialTitle;
    public string TopicLine => string.IsNullOrWhiteSpace(Attempt.MaterialSection)
        ? Attempt.MaterialTopic
        : $"{Attempt.MaterialTopic} • {Attempt.MaterialSection}";
    public string ScoreLine => $"{Attempt.Score:0.##} / {Attempt.MaxScore:0.##} баллов";
    public string PercentLine => $"{Attempt.Percent:0.##}%";
    public string GradeLine => TestGrading.FormatGrade(Attempt.Grade);
    public string DurationLine
    {
        get
        {
            var duration = Attempt.CompletedAtUtc - Attempt.StartedAtUtc;
            if (duration.TotalMinutes < 1) return $"Время: {Math.Max(1, (int)duration.TotalSeconds)} сек.";
            return $"Время: {(int)duration.TotalMinutes} мин. {duration.Seconds} сек.";
        }
    }

    public string StatusLine => Attempt.Status switch
    {
        SubmissionStatus.NeedsReview => "Часть ответов отправлена на ручную проверку.",
        SubmissionStatus.AutoChecked => "Все ответы проверены автоматически.",
        _ => "Тест завершён."
    };

    public int CorrectCount => Questions.Count(x => x.IsCorrect);
    public int IncorrectCount => Questions.Count(x => x.IsIncorrect);
    public int PartialCount => Questions.Count(x => x.IsPartial);
    public int PendingCount => Questions.Count(x => x.IsPending);
    public string SummaryLine => $"Верно: {CorrectCount} • Ошибки: {IncorrectCount} • Частично: {PartialCount} • На проверке: {PendingCount}";
}

public sealed class TestQuestionResultViewModel
{
    public TestQuestionResultViewModel(int number, SubmissionDto submission)
    {
        Number = number;
        TaskTitle = submission.TaskTitle;
        Answer = string.IsNullOrWhiteSpace(submission.Answer) ? "—" : submission.Answer;
        ScoreLine = $"{submission.Score:0.##} / {submission.MaxScore:0.##}";
        Feedback = string.IsNullOrWhiteSpace(submission.Feedback) ? "Комментарий отсутствует." : submission.Feedback;
        Status = submission.Status;
        IsCorrect = submission.MaxScore > 0 && submission.Score >= submission.MaxScore && submission.Status is SubmissionStatus.AutoChecked or SubmissionStatus.Reviewed;
        IsPartial = submission.MaxScore > 0 && submission.Score > 0 && submission.Score < submission.MaxScore;
        IsPending = submission.Status is SubmissionStatus.NeedsReview or SubmissionStatus.Submitted;
        IsIncorrect = !IsCorrect && !IsPartial && !IsPending;
    }

    public int Number { get; }
    public string TaskTitle { get; }
    public string Answer { get; }
    public string AnswerLine => $"Ваш ответ: {Answer}";
    public string ScoreLine { get; }
    public string Feedback { get; }
    public SubmissionStatus Status { get; }
    public bool IsCorrect { get; }
    public bool IsPartial { get; }
    public bool IsPending { get; }
    public bool IsIncorrect { get; }

    public string VerdictText => IsCorrect ? "Верно" : IsPartial ? "Частично" : IsPending ? "На проверке" : "Неверно";
}
