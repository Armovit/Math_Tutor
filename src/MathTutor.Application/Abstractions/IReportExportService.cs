using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IReportExportService
{
    Task ExportStudentProgressAsync(ProgressSummaryDto progress, Stream output, CancellationToken cancellationToken = default);
    Task ExportAdminStatisticsAsync(AdminStatisticsDto statistics, Stream output, CancellationToken cancellationToken = default);
}
