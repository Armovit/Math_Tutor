using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface ITestService
{
    Task<OperationResult<TestAttemptDto>> SubmitTestAsync(SubmitTestRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestAttemptDto>> GetAttemptsAsync(int userId, int? materialId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestAttemptDto>> GetAttemptsAsync(TestAttemptQuery query, CancellationToken cancellationToken = default);
}
