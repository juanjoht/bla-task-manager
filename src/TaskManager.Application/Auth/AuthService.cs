using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;

namespace TaskManager.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    TimeProvider timeProvider)
{
    public Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
