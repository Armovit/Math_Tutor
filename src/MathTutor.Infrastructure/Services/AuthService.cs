using System.Net.Mail;
using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class AuthService(MathTutorDbContext dbContext, IPasswordHasher passwordHasher, IPasswordValidator passwordValidator) : IAuthService
{
    public async Task<OperationResult<UserSessionDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return OperationResult<UserSessionDto>.Failure("Введите имя и фамилию.");
        }

        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
        {
            return OperationResult<UserSessionDto>.Failure("Введите корректный email.");
        }

        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return OperationResult<UserSessionDto>.Failure("Пользователь с таким email уже существует.");
        }

        var passwordResult = passwordValidator.Validate(request.Password);
        if (!passwordResult.Succeeded)
        {
            return OperationResult<UserSessionDto>.Failure(passwordResult.Message);
        }

        if (request.Password != request.ConfirmPassword)
        {
            return OperationResult<UserSessionDto>.Failure("Пароли не совпадают.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Student,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<UserSessionDto>.Success(ToSession(user), "Регистрация выполнена.");
    }

    public async Task<OperationResult<UserSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return OperationResult<UserSessionDto>.Failure("Неверный email или пароль.");
        }

        if (user.IsBlocked)
        {
            return OperationResult<UserSessionDto>.Failure("Пользователь заблокирован. Обратитесь к администратору.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<UserSessionDto>.Success(ToSession(user), "Вход выполнен.");
    }

    private static UserSessionDto ToSession(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.Role, user.IsBlocked);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        try { _ = new MailAddress(email); return true; }
        catch { return false; }
    }
}
