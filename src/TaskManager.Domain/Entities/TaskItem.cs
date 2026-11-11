using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

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

    // Required by EF Core to materialize entities from the database.
    private TaskItem()
    {
    }

    public static TaskItem Create(Guid userId, string title, string? description, DateOnly dueDate, DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var validTitle = ValidateTitle(title);
        var validDescription = ValidateDescription(description);

        if (dueDate < DateOnly.FromDateTime(utcNow))
        {
            throw new DomainException("Due date cannot be in the past.");
        }

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = validTitle,
            Description = validDescription,
            Status = TaskItemStatus.Pending,
            DueDate = dueDate,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Update(string title, string? description, TaskItemStatus status, DateOnly dueDate, DateTime utcNow)
    {
        // Validate everything before mutating so a failed update leaves the task unchanged.
        var validTitle = ValidateTitle(title);
        var validDescription = ValidateDescription(description);

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Status is not valid.");
        }

        Title = validTitle;
        Description = validDescription;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = utcNow;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainException($"Title cannot exceed {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Description cannot exceed {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }
}
