using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Abstractions.Security;

namespace TaskManager.Infrastructure.Security;

/// <summary>Wraps ASP.NET Core Identity's hasher (PBKDF2 with a random salt per hash).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    // Identity's hasher is generic over a user type but does not use it, so any placeholder works.
    private static readonly object NoUser = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
