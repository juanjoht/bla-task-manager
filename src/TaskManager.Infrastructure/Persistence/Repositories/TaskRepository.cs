using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, TaskItemStatus? status, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken) => throw new NotImplementedException();
}
