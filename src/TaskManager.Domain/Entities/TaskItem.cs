using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

public sealed class TaskItem
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem()
    {
    }

    public static TaskItem Create(Guid userId, string title, string? description, DateOnly dueDate, DateTime utcNow) =>
        throw new NotImplementedException();

    public void Update(string title, string? description, TaskItemStatus status, DateOnly dueDate, DateTime utcNow) =>
        throw new NotImplementedException();
}
