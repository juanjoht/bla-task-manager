namespace TaskManager.Application.Dtos.Tasks;

public sealed record CreateTaskRequest(string Title, string? Description, DateOnly DueDate);
