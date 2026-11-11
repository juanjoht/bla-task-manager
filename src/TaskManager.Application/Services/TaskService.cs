using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public sealed class TaskService(ITaskRepository tasks, IClock clock)
{
    public async Task<IReadOnlyList<TaskDto>> ListAsync(Guid userId, TaskItemStatus? status, CancellationToken cancellationToken)
    {
        var userTasks = await tasks.ListByUserAsync(userId, status, cancellationToken);
        return userTasks.Select(ToDto).ToList();
    }

    public async Task<TaskDto> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        return ToDto(task);
    }

    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskItem(userId, request.Title, request.Description, request.DueDate, clock.Today, clock.UtcNow);

        await tasks.AddAsync(task, cancellationToken);
        return ToDto(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        task.Update(request.Title, request.Description, request.Status, request.DueDate, clock.UtcNow);

        await tasks.UpdateAsync(task, cancellationToken);
        return ToDto(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        await tasks.DeleteAsync(task, cancellationToken);
    }

    // Same 404 for "does not exist" and "belongs to another user", so other users' task ids cannot be discovered.
    private async Task<TaskItem> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.UserId != userId)
        {
            throw new NotFoundException("Task not found.");
        }

        return task;
    }

    private static TaskDto ToDto(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt);
}
