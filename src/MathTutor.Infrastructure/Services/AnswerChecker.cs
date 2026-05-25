using System.Globalization;
using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;

namespace MathTutor.Infrastructure.Services;

internal readonly record struct AnswerCheckResult(decimal Score, SubmissionStatus Status, string Feedback);

internal static class AnswerChecker
{
    public static AnswerCheckResult Check(MathTask task, string answer)
    {
        if (task.AnswerType == AnswerType.ManualReview || string.IsNullOrWhiteSpace(task.CorrectAnswer))
        {
            return new AnswerCheckResult(0, SubmissionStatus.NeedsReview, "Решение ожидает ручной проверки.");
        }

        var isCorrect = task.AnswerType switch
        {
            AnswerType.Number => IsNumberCorrect(task.CorrectAnswer, answer),
            AnswerType.SingleChoice or AnswerType.MultipleChoice => NormalizeChoice(task.CorrectAnswer) == NormalizeChoice(answer),
            _ => string.Equals(task.CorrectAnswer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase)
        };

        return isCorrect
            ? new AnswerCheckResult(task.MaxScore, SubmissionStatus.AutoChecked, "Ответ верный.")
            : new AnswerCheckResult(0, SubmissionStatus.AutoChecked, $"Ответ неверный. Правильный ответ: {task.CorrectAnswer}.");
    }

    private static bool IsNumberCorrect(string expected, string actual)
    {
        return decimal.TryParse(expected.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedNumber)
            && decimal.TryParse(actual.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var actualNumber)
            && Math.Abs(expectedNumber - actualNumber) <= 0.001m;
    }

    private static string NormalizeChoice(string value) => string.Join(';', value.Split(';', ',', '|').Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0).OrderBy(x => x));
}
