using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Security;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
