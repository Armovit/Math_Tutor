using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IProgressService
{
    Task<ProgressSummaryDto> GetStudentProgressAsync(int userId, CancellationToken cancellationToken = default);
    Task<AdminStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default);
}
