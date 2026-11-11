using TaskManager.Application.Abstractions.Security;

namespace TaskManager.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => throw new NotImplementedException();

    public bool Verify(string password, string passwordHash) => throw new NotImplementedException();
}
