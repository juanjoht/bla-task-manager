using TaskManager.Domain.Enums;

namespace TaskManager.Application.Dtos.Tasks;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
