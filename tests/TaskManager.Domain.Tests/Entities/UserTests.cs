using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private const string ValidHash = "hashed-password";

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var user = User.Create("demo@ballastlane.test", "Demo User", ValidHash, UtcNow);

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("demo@ballastlane.test");
        user.Name.Should().Be("Demo User");
        user.PasswordHash.Should().Be(ValidHash);
        user.CreatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_NormalizesEmailToTrimmedLowercase()
    {
        // Normalizing here makes "Demo@X.com" and "demo@x.com" the same account for the unique check.
        var user = User.Create("  Demo@BallastLane.TEST ", "Demo User", ValidHash, UtcNow);

        user.Email.Should().Be("demo@ballastlane.test");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingEmail_Throws(string? email)
    {
        var act = () => User.Create(email!, "Demo User", ValidHash, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Email is required.");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing-at.com")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("user@domain")]
    [InlineData("us er@domain.com")]
    [InlineData("user@@domain.com")]
    public void Create_WithInvalidEmailFormat_Throws(string email)
    {
        var act = () => User.Create(email, "Demo User", ValidHash, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Email format is not valid.");
    }

    [Fact]
    public void Create_WithEmailOverMaxLength_Throws()
    {
        const string domain = "@x.com";
        var email = new string('a', User.EmailMaxLength - domain.Length + 1) + domain;

        var act = () => User.Create(email, "Demo User", ValidHash, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Email cannot exceed 254 characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_Throws(string? name)
    {
        var act = () => User.Create("demo@ballastlane.test", name!, ValidHash, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Name is required.");
    }

    [Fact]
    public void Create_TrimsName()
    {
        var user = User.Create("demo@ballastlane.test", "  Demo User  ", ValidHash, UtcNow);

        user.Name.Should().Be("Demo User");
    }

    [Fact]
    public void Create_WithNameOverMaxLength_Throws()
    {
        var name = new string('a', User.NameMaxLength + 1);

        var act = () => User.Create("demo@ballastlane.test", name, ValidHash, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Name cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingPasswordHash_Throws(string? passwordHash)
    {
        var act = () => User.Create("demo@ballastlane.test", "Demo User", passwordHash!, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Password hash is required.");
    }
}
