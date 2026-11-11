using TaskManager.Domain.Common;
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

    public TaskItem(Guid userId, string title, string? description, DateOnly dueDate, DateOnly today, DateTime utcNow)
    {
        Guard.NotEmpty(userId, "User id");
        Guard.NotInPast(dueDate, utcNow, "Due date");

        Id = Guid.NewGuid();
        UserId = userId;
        Title = Guard.RequiredMaxLength(title, TitleMaxLength, "Title");
        Description = Guard.OptionalMaxLength(description, DescriptionMaxLength, "Description");
        Status = TaskItemStatus.Pending;
        DueDate = dueDate;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    // Used only by EF Core to load existing rows (it must not re-run creation rules such as "due date not in the past").
    private TaskItem()
    {
    }

    public void Update(string title, string? description, TaskItemStatus status, DateOnly dueDate, DateTime utcNow)
    {
        // Validate everything first, then assign: a failed update leaves the task unchanged.
        var validTitle = Guard.RequiredMaxLength(title, TitleMaxLength, "Title");
        var validDescription = Guard.OptionalMaxLength(description, DescriptionMaxLength, "Description");
        Guard.Defined(status, "Status");

        Title = validTitle;
        Description = validDescription;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = utcNow;
    }
}
