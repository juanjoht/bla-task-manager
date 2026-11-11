using FluentAssertions;
using Moq;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Auth;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tests.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Auth;

public class AuthServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private const string HashedPassword = "hashed-password";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(HashedPassword);
        _sut = new AuthService(_users.Object, _passwordHasher.Object, _tokenGenerator.Object, new FixedTimeProvider(UtcNow));
    }

    private static User ExistingUser() => new("demo@ballastlane.test", "Demo User", HashedPassword, UtcNow);

    // ---------- Register ----------

    [Fact]
    public async Task Register_WithNewEmail_SavesUserWithHashedPassword()
    {
        var request = new RegisterRequest("demo@ballastlane.test", "Demo User", "Demo123!");

        await _sut.RegisterAsync(request, CancellationToken.None);

        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "demo@ballastlane.test"
                             && u.Name == "Demo User"
                             && u.PasswordHash == HashedPassword
                             && u.CreatedAt == UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_NeverStoresThePlainPassword()
    {
        var request = new RegisterRequest("demo@ballastlane.test", "Demo User", "Demo123!");

        await _sut.RegisterAsync(request, CancellationToken.None);

        _passwordHasher.Verify(h => h.Hash("Demo123!"), Times.Once);
        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.PasswordHash != "Demo123!"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsTheCreatedUser()
    {
        var request = new RegisterRequest("demo@ballastlane.test", "Demo User", "Demo123!");

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Email.Should().Be("demo@ballastlane.test");
        result.Name.Should().Be("Demo User");
    }

    [Fact]
    public async Task Register_ChecksDuplicatesUsingTheNormalizedEmail()
    {
        var request = new RegisterRequest("  Demo@BallastLane.TEST ", "Demo User", "Demo123!");

        await _sut.RegisterAsync(request, CancellationToken.None);

        _users.Verify(r => r.EmailExistsAsync("demo@ballastlane.test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsConflictAndDoesNotSave()
    {
        _users.Setup(r => r.EmailExistsAsync("demo@ballastlane.test", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new RegisterRequest("demo@ballastlane.test", "Demo User", "Demo123!");

        var act = () => _sut.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("Email is already registered.");
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.test", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Demo123!", HashedPassword)).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken(user)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest("demo@ballastlane.test", "Demo123!"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        result.User.Should().Be(new UserDto(user.Id, user.Email, user.Name));
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveOnEmail()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.test", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Demo123!", HashedPassword)).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken(user)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest(" DEMO@BallastLane.test ", "Demo123!"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorized()
    {
        var act = () => _sut.LoginAsync(new LoginRequest("nobody@ballastlane.test", "Demo123!"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsTheSameErrorAndIssuesNoToken()
    {
        // Same message as "unknown email" so the API never reveals which emails are registered.
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.test", It.IsAny<CancellationToken>())).ReturnsAsync(ExistingUser());
        _passwordHasher.Setup(h => h.Verify("wrong", HashedPassword)).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("demo@ballastlane.test", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Invalid email or password.");
        _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    // ---------- Current user ----------

    [Fact]
    public async Task GetCurrentUser_ReturnsUserWithoutPasswordHash()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Should().Be(new UserDto(user.Id, "demo@ballastlane.test", "Demo User"));
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNoLongerExists_ThrowsUnauthorized()
    {
        var act = () => _sut.GetCurrentUserAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("User not found.");
    }
}
