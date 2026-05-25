using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class UserManagementService(MathTutorDbContext dbContext) : IUserManagementService
{
    public Task<IReadOnlyList<UserDto>> SearchUsersAsync(string? search, CancellationToken cancellationToken = default)
        => SearchUsersDetailedAsync(new UserQuery(search), cancellationToken);

    public async Task<IReadOnlyList<UserDto>> SearchUsersDetailedAsync(UserQuery request, CancellationToken cancellationToken = default)
    {
        var users = dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalized = request.Search.Trim().ToLower();
            users = users.Where(x => x.FirstName.ToLower().Contains(normalized) || x.LastName.ToLower().Contains(normalized) || x.Email.ToLower().Contains(normalized));
        }

        if (request.Role is not null) users = users.Where(x => x.Role == request.Role);
        if (request.IsBlocked is not null) users = users.Where(x => x.IsBlocked == request.IsBlocked);

        var query = users.Select(x => new UserDto(
            x.Id,
            x.FirstName,
            x.LastName,
            x.Email,
            x.Role,
            x.IsBlocked,
            x.CreatedAtUtc,
            x.LastLoginAtUtc,
            x.TaskSubmissions.Select(s => s.MathTaskId).Distinct().Count(),
            x.TestAttempts.Count(),
            x.TestAttempts.Any() ? x.TestAttempts.Average(a => a.Percent) : 0,
            x.Reviews.Count()));

        var result = await query.ToListAsync(cancellationToken);

        IEnumerable<UserDto> ordered = request.SortBy switch
        {
            UserSortBy.Email => request.SortDescending ? result.OrderByDescending(x => x.Email) : result.OrderBy(x => x.Email),
            UserSortBy.Role => request.SortDescending ? result.OrderByDescending(x => x.Role) : result.OrderBy(x => x.Role),
            UserSortBy.CreatedAt => request.SortDescending ? result.OrderByDescending(x => x.CreatedAtUtc) : result.OrderBy(x => x.CreatedAtUtc),
            UserSortBy.LastLogin => request.SortDescending ? result.OrderByDescending(x => x.LastLoginAtUtc) : result.OrderBy(x => x.LastLoginAtUtc),
            UserSortBy.CompletedTasks => request.SortDescending ? result.OrderByDescending(x => x.CompletedTasks) : result.OrderBy(x => x.CompletedTasks),
            UserSortBy.TestAttempts => request.SortDescending ? result.OrderByDescending(x => x.TestAttemptsCount) : result.OrderBy(x => x.TestAttemptsCount),
            UserSortBy.AverageTestPercent => request.SortDescending ? result.OrderByDescending(x => x.AverageTestPercent) : result.OrderBy(x => x.AverageTestPercent),
            UserSortBy.Reviews => request.SortDescending ? result.OrderByDescending(x => x.ReviewsCount) : result.OrderBy(x => x.ReviewsCount),
            UserSortBy.Blocked => request.SortDescending ? result.OrderByDescending(x => x.IsBlocked) : result.OrderBy(x => x.IsBlocked),
            _ => request.SortDescending ? result.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName) : result.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
        };

        if (request.Skip is > 0) ordered = ordered.Skip(request.Skip.Value);
        if (request.Take is > 0) ordered = ordered.Take(request.Take.Value);

        return ordered.ToList();
    }

    public async Task<OperationResult> SetBlockedAsync(int userId, bool isBlocked, int currentAdminId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null) return OperationResult.Failure("Пользователь не найден.");
        if (user.Id == currentAdminId) return OperationResult.Failure("Нельзя заблокировать собственную учётную запись.");
        if (user.Role == UserRole.Admin && isBlocked && await dbContext.Users.CountAsync(x => x.Role == UserRole.Admin && !x.IsBlocked, cancellationToken) <= 1)
        {
            return OperationResult.Failure("Нельзя заблокировать единственного активного администратора.");
        }

        user.IsBlocked = isBlocked;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success(isBlocked ? "Пользователь заблокирован." : "Пользователь разблокирован.");
    }
}