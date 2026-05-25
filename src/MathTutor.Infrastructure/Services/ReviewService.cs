using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class ReviewService(MathTutorDbContext dbContext) : IReviewService
{
    public async Task<IReadOnlyList<ReviewDto>> GetReviewsAsync(int? materialId = null, int? taskId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Reviews
            .Include(x => x.User)
            .Include(x => x.EducationalMaterial)
            .Include(x => x.MathTask)
            .AsNoTracking()
            .AsQueryable();
        if (materialId is not null) query = query.Where(x => x.EducationalMaterialId == materialId);
        if (taskId is not null) query = query.Where(x => x.MathTaskId == taskId);
        return await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<ReviewDto>> AddReviewAsync(ReviewEditDto review, CancellationToken cancellationToken = default)
    {
        if (review.EducationalMaterialId is null && review.MathTaskId is null) return OperationResult<ReviewDto>.Failure("Выберите материал или задание для отзыва.");
        if (review.DifficultyRating is < 1 or > 5 || review.UsefulnessRating is < 1 or > 5) return OperationResult<ReviewDto>.Failure("Оценки должны быть от 1 до 5.");

        var entity = new Review
        {
            UserId = review.UserId,
            EducationalMaterialId = review.EducationalMaterialId,
            MathTaskId = review.MathTaskId,
            DifficultyRating = review.DifficultyRating,
            UsefulnessRating = review.UsefulnessRating,
            Comment = review.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.Reviews.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.EducationalMaterial).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.MathTask).LoadAsync(cancellationToken);
        return OperationResult<ReviewDto>.Success(ToDto(entity), "Спасибо за отзыв.");
    }

    private static ReviewDto ToDto(Review x) => new(
        x.Id,
        x.UserId,
        x.User?.FullName ?? string.Empty,
        x.EducationalMaterialId,
        x.EducationalMaterial?.Title ?? string.Empty,
        x.EducationalMaterial?.Topic ?? string.Empty,
        x.EducationalMaterial?.Section ?? string.Empty,
        x.EducationalMaterial?.Type,
        x.MathTaskId,
        x.MathTask?.Title ?? string.Empty,
        x.DifficultyRating,
        x.UsefulnessRating,
        x.Comment,
        x.CreatedAtUtc);
}
