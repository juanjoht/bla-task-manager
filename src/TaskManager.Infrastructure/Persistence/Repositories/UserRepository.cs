using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();
}
