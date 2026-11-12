using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Security;

public sealed class JwtTokenGenerator(JwtOptions options, IClock clock) : ITokenGenerator
{
    public string GenerateToken(User user) => throw new NotImplementedException();
}
