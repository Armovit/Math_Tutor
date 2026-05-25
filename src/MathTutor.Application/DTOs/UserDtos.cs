using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public enum UserSortBy
{
    FullName = 1,
    Email = 2,
    Role = 3,
    CreatedAt = 4,
    LastLogin = 5,
    CompletedTasks = 6,
    TestAttempts = 7,
    AverageTestPercent = 8,
    Reviews = 9,
    Blocked = 10
}

public sealed record UserQuery(
    string? Search = null,
    UserRole? Role = null,
    bool? IsBlocked = null,
    UserSortBy SortBy = UserSortBy.FullName,
    bool SortDescending = false,
    int? Skip = null,
    int? Take = null);

public sealed record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsBlocked,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    int CompletedTasks = 0,
    int TestAttemptsCount = 0,
    decimal AverageTestPercent = 0,
    int ReviewsCount = 0)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string ActivitySummary => $"{CompletedTasks} решений · {TestAttemptsCount} тестов · {ReviewsCount} отзывов";
    public string StatusText => IsBlocked ? "Заблокирован" : "Активен";
}
