using ClosedXML.Excel;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;

namespace MathTutor.Infrastructure.Services;

public sealed class ReportExportService : IReportExportService
{
    public Task ExportStudentProgressAsync(ProgressSummaryDto progress, Stream output, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Сводка");
        summary.Cell(1, 1).Value = "Показатель";
        summary.Cell(1, 2).Value = "Значение";
        summary.Cell(2, 1).Value = "Выполнено заданий";
        summary.Cell(2, 2).Value = progress.CompletedTasks;
        summary.Cell(3, 1).Value = "Средний результат, %";
        summary.Cell(3, 2).Value = progress.AverageScore;
        summary.Cell(4, 1).Value = "Лучший тест, %";
        summary.Cell(4, 2).Value = progress.BestTestPercent;
        summary.Cell(5, 1).Value = "Средний тест, %";
        summary.Cell(5, 2).Value = progress.AverageTestPercent;
        summary.Columns().AdjustToContents();

        var topics = workbook.Worksheets.Add("Темы");
        topics.Cell(1, 1).Value = "Тема";
        topics.Cell(1, 2).Value = "Средний %";
        topics.Cell(1, 3).Value = "Лучший %";
        topics.Cell(1, 4).Value = "Попытки";
        topics.Cell(1, 5).Value = "Выполнено задач";
        topics.Cell(1, 6).Value = "Всего задач";
        topics.Cell(1, 7).Value = "Охват темы, %";
        for (var i = 0; i < progress.TopicProgress.Count; i++)
        {
            var item = progress.TopicProgress[i];
            var row = i + 2;
            topics.Cell(row, 1).Value = item.Topic;
            topics.Cell(row, 2).Value = item.AveragePercent;
            topics.Cell(row, 3).Value = item.BestPercent;
            topics.Cell(row, 4).Value = item.AttemptCount;
            topics.Cell(row, 5).Value = item.CompletedTasks;
            topics.Cell(row, 6).Value = item.TotalTasks;
            topics.Cell(row, 7).Value = item.CompletionPercent;
        }
        topics.Columns().AdjustToContents();

        var attempts = workbook.Worksheets.Add("Тесты");
        attempts.Cell(1, 1).Value = "Материал";
        attempts.Cell(1, 2).Value = "Тема";
        attempts.Cell(1, 3).Value = "Процент";
        attempts.Cell(1, 4).Value = "Оценка";
        attempts.Cell(1, 5).Value = "Дата";
        for (var i = 0; i < progress.RecentTestAttempts.Count; i++)
        {
            var item = progress.RecentTestAttempts[i];
            var row = i + 2;
            attempts.Cell(row, 1).Value = item.MaterialTitle;
            attempts.Cell(row, 2).Value = item.MaterialTopic;
            attempts.Cell(row, 3).Value = item.Percent;
            attempts.Cell(row, 4).Value = item.Grade;
            attempts.Cell(row, 5).Value = item.CompletedAtUtc;
        }
        attempts.Columns().AdjustToContents();

        workbook.SaveAs(output);
        return Task.CompletedTask;
    }

    public Task ExportAdminStatisticsAsync(AdminStatisticsDto statistics, Stream output, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Сводка");
        summary.Cell(1, 1).Value = "Показатель";
        summary.Cell(1, 2).Value = "Значение";
        summary.Cell(2, 1).Value = "Ученики";
        summary.Cell(2, 2).Value = statistics.StudentsCount;
        summary.Cell(3, 1).Value = "Материалы";
        summary.Cell(3, 2).Value = statistics.MaterialsCount;
        summary.Cell(4, 1).Value = "Опубликовано";
        summary.Cell(4, 2).Value = statistics.PublishedMaterialsCount;
        summary.Cell(5, 1).Value = "Решения";
        summary.Cell(5, 2).Value = statistics.SubmissionsCount;
        summary.Cell(6, 1).Value = "Средний результат, %";
        summary.Cell(6, 2).Value = statistics.AverageScore;
        summary.Columns().AdjustToContents();

        var students = workbook.Worksheets.Add("Ученики");
        students.Cell(1, 1).Value = "Ученик";
        students.Cell(1, 2).Value = "Выполнено заданий";
        students.Cell(1, 3).Value = "Средний %";
        students.Cell(1, 4).Value = "Прогресс, %";
        for (var i = 0; i < statistics.Students.Count; i++)
        {
            var item = statistics.Students[i];
            var row = i + 2;
            students.Cell(row, 1).Value = item.FullName;
            students.Cell(row, 2).Value = item.CompletedTasks;
            students.Cell(row, 3).Value = item.AverageScore;
            students.Cell(row, 4).Value = item.CompletionPercent;
        }
        students.Columns().AdjustToContents();

        workbook.SaveAs(output);
        return Task.CompletedTask;
    }
}
