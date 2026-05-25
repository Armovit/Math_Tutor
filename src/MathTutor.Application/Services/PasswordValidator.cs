using System.Text.RegularExpressions;
using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;

namespace MathTutor.Application.Services;

public sealed class PasswordValidator : IPasswordValidator
{
    private static readonly Regex Lowercase = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex Uppercase = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex Digit = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex Special = new("[^a-zA-Z0-9]", RegexOptions.Compiled);
    private static readonly Regex LatinOnly = new("^[\x21-\x7E]+$", RegexOptions.Compiled);

    public OperationResult Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return OperationResult.Failure("Введите пароль.");
        }

        if (password.Length is < 8 or > 20)
        {
            return OperationResult.Failure("Пароль должен содержать от 8 до 20 символов.");
        }

        if (!LatinOnly.IsMatch(password))
        {
            return OperationResult.Failure("Пароль должен содержать только латинские символы, цифры и спецсимволы.");
        }

        if (!Lowercase.IsMatch(password) || !Uppercase.IsMatch(password) || !Digit.IsMatch(password) || !Special.IsMatch(password))
        {
            return OperationResult.Failure("Пароль должен содержать строчную и заглавную латинскую букву, цифру и спецсимвол.");
        }

        return OperationResult.Success("Пароль подходит.");
    }
}
