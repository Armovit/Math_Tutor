using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetReviewsAsync(int? materialId = null, int? taskId = null, CancellationToken cancellationToken = default);
    Task<OperationResult<ReviewDto>> AddReviewAsync(ReviewEditDto review, CancellationToken cancellationToken = default);
}
