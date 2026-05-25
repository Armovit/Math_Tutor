using FluentAssertions;
using MathTutor.Application.Services;

namespace MathTutor.UnitTests.Auth;

public sealed class PasswordValidatorTests
{
    [Theory]
    [InlineData("Admin123!")]
    [InlineData("Student9#")]
    public void Validate_accepts_strong_latin_passwords(string password)
    {
        var result = new PasswordValidator().Validate(password);
        result.Succeeded.Should().BeTrue(result.Message);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("withoutdigit!")]
    [InlineData("WITHOUTDIGIT1!")]
    [InlineData("NoSpecial123")]
    [InlineData("Пароль123!")]
    public void Validate_rejects_passwords_that_do_not_match_policy(string password)
    {
        var result = new PasswordValidator().Validate(password);
        result.Succeeded.Should().BeFalse();
    }
}
