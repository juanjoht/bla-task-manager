using TaskManager.Domain.Enums;

namespace TaskManager.Application.Dtos.Tasks;

public sealed record UpdateTaskRequest(string Title, string? Description, TaskItemStatus Status, DateOnly DueDate);
