using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public sealed class TaskService(ITaskRepository tasks, IClock clock)
{
    public Task<IReadOnlyList<TaskDto>> ListAsync(Guid userId, TaskItemStatus? status, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskDto> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
