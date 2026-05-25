using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> SearchUsersDetailedAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<OperationResult> SetBlockedAsync(int userId, bool isBlocked, int currentAdminId, CancellationToken cancellationToken = default);
}
