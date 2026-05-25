using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IAuthService
{
    Task<OperationResult<UserSessionDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<UserSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
