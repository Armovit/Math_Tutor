using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;

namespace MathTutor.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    public UserSessionDto? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public event EventHandler? SessionChanged;

    public void SetCurrentUser(UserSessionDto user)
    {
        CurrentUser = user;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        CurrentUser = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
