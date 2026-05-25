using FluentAssertions;
using MathTutor.Application.Services;

namespace MathTutor.UnitTests.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_never_returns_plain_password_and_verifies_original_value()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Admin123!");
        hash.Should().NotBe("Admin123!");
        hasher.Verify("Admin123!", hash).Should().BeTrue();
        hasher.Verify("Wrong123!", hash).Should().BeFalse();
    }
}
