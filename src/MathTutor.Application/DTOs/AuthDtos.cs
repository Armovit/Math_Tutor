using MathTutor.Domain.Enums;

namespace MathTutor.Application.DTOs;

public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);
public sealed record LoginRequest(string Email, string Password);
public sealed record UserSessionDto(int Id, string FirstName, string LastName, string Email, UserRole Role, bool IsBlocked)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
