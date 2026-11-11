using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    TimeProvider timeProvider)
{
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = new User(request.Email, request.Name, passwordHash, timeProvider.GetUtcNow().UtcDateTime);

        await users.AddAsync(user, cancellationToken);
        return ToDto(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        // Same error for "unknown email" and "wrong password" so registered emails cannot be discovered.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        return new AuthResponse(tokenGenerator.GenerateToken(user), ToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.Name);
}
