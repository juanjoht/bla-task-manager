using FluentAssertions;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NeverReturnsThePlainPassword()
    {
        var hash = _hasher.Hash("Demo123!");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotContain("Demo123!");
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // A random salt per hash: two users with the same password get different hashes.
        var first = _hasher.Hash("Demo123!");
        var second = _hasher.Hash("Demo123!");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_WithTheCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Demo123!");

        _hasher.Verify("Demo123!", hash).Should().BeTrue();
    }

    [Theory]
    [InlineData("Wrong123!")]
    [InlineData("demo123!")]
    [InlineData("")]
    public void Verify_WithAWrongPassword_ReturnsFalse(string attempt)
    {
        var hash = _hasher.Hash("Demo123!");

        _hasher.Verify(attempt, hash).Should().BeFalse();
    }
}
