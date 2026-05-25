using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface ISessionService
{
    UserSessionDto? CurrentUser { get; }
    bool IsAuthenticated { get; }
    event EventHandler? SessionChanged;
    void SetCurrentUser(UserSessionDto user);
    void Clear();
}
